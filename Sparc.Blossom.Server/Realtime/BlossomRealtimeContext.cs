using Ardalis.Specification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Sparc.Blossom.Api;
using System.Reflection;

namespace Sparc.Blossom.Realtime;

public class BlossomRealtimeContext(NavigationManager nav)
{
    public HubConnection? Connection { get; set; }

    public bool IsOn { get; private set; }
    public bool IsConnected => Connection?.State == HubConnectionState.Connected;
    public bool HasError;

    readonly List<IDisposable> Events = [];
    readonly Dictionary<string, int> Subscriptions = [];
    public EventCallback<HubConnection>? OnConnected;
    public event EventHandler<EventArgs>? Changed;
    
    public void StateHasChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public void Initialize(bool isOn, EventCallback<HubConnection>? onConnected = null)
    {
        IsOn = isOn;
        
        Connection ??= new HubConnectionBuilder()
            .WithUrl($"{nav.BaseUri}_realtime")
            //.AddMessagePackProtocol()
            .WithAutomaticReconnect()
            .Build();

        if (onConnected != null)
            OnConnected = onConnected;
    }

    public async Task GoOnline()
    {
        if (!IsOn)
            return;

        if (Connection == null && IsOn)
            Initialize(IsOn);

        if (Connection?.State != HubConnectionState.Disconnected)
            return;

        var attempts = 5;
        HasError = false;

        // Keep trying to connect until we can start or the token is canceled.
        while (attempts > 0)
        {
            try
            {
                await Connection.StartAsync();
                StateHasChanged();
                if (OnConnected != null)
                    await OnConnected.Value.InvokeAsync(Connection);

                return;
            }
            catch (Exception)
            {
                // Failed to connect, trying again in 3000 ms.
                await Task.Delay(3000);
                attempts--;
            }
        }

        HasError = true;
    }

    public async Task GoOnline(ComponentBase component)
    {
        Initialize(true);
        var type = component.GetType();

        var properties = type.GetProperties().OfType<IBlossomEntityProxy>();
        await Watch(properties);

        var stateHasChanged = type.GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance);
        Changed += (sender, args) => stateHasChanged!.Invoke(component, null);
    }

    public Task GoOnline(IBlossomEntityProxy entity)
    {
        entity.IsLive = true;
        return Task.CompletedTask;
    }

    public async Task Watch(IBlossomEntityProxy entity) => await Watch([entity]);

    public async Task Watch(IEnumerable<IBlossomEntityProxy> entities)
    {
        await On(entities, (e, ev) => e.Patch(ev.Changes!));
    }

    public async Task StopWatching(IBlossomEntityProxy entity)
    {
        if (Subscriptions.TryGetValue(entity.SubscriptionId, out int value))
        {
            Subscriptions[entity.SubscriptionId] = --value;
            if (value <= 0)
            {
                Subscriptions.Remove(entity.SubscriptionId);
                await InvokeAsync("StopWatching", entity.SubscriptionId);
            }
        }
    }

    public async Task On(IEnumerable<IBlossomEntityProxy> entities, Action<IBlossomEntityProxy, BlossomEvent> action)
    {
        if (!IsOn)
            return;
        
        await GoOnline();
        
        var subscriptionIds = entities.Select(x => x.SubscriptionId).ToList();
        var newSubscriptions = subscriptionIds.Except(Subscriptions.Keys);
        foreach (var subscriptionId in newSubscriptions)
            Subscriptions.Add(subscriptionId, 0);

        foreach (var entity in entities)
        {
            Subscriptions[entity.SubscriptionId]++;

            Events.Add(Connection!.On<BlossomEvent>(entity.SubscriptionId, evt =>
            {
                action(entity, evt);
                StateHasChanged();
            }));
        }

        if (subscriptionIds.Count != 0)
            await InvokeAsync("Watch", subscriptionIds);
    }

    private async Task InvokeAsync<T>(string method, T parameters)
    {
        if (IsConnected)
            await Connection!.InvokeAsync(method, parameters);
        else
            Connection!.On("_UserConnected", async () =>
            {
                await Connection!.InvokeAsync(method, parameters);
            });
    }

    public async Task GoOffline()
    {
        if (Connection != null)
            await Connection.DisposeAsync();

        foreach (var evt in Events)
            evt.Dispose();

        Events.Clear();
        Subscriptions.Clear();
    }

    public async Task GoOffline(ComponentBase component)
    {
        var properties = component.GetType().GetProperties();
        foreach (var property in properties.OfType<IBlossomEntityProxy>())
            await StopWatching(property);
    }

    public Task GoOffline(IBlossomEntityProxy entity)
    {
        entity.IsLive = false;
        return Task.CompletedTask;
    }
}
