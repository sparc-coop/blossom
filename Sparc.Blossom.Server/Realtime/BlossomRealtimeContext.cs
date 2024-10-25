using Ardalis.Specification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Sparc.Blossom.Api;

namespace Sparc.Blossom.Realtime;

public class BlossomRealtimeContext(NavigationManager nav)
{
    public HubConnection? Connection { get; set; }

    public bool IsConnected => Connection?.State == HubConnectionState.Connected;
    public bool HasError;

    readonly List<IDisposable> Events = [];
    readonly Dictionary<string, int> Subscriptions = [];
    public EventCallback<HubConnection>? OnConnected;
    public event EventHandler<EventArgs>? Changed;
    
    public void StateHasChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public void Initialize(EventCallback<HubConnection>? onConnected = null)
    {
         Connection ??= new HubConnectionBuilder()
            .WithUrl($"{nav.BaseUri}_realtime")
            //.AddMessagePackProtocol()
            .WithAutomaticReconnect()
            .Build();

        if (onConnected != null)
            OnConnected = onConnected;
    }

    public async Task InitializeAsync(ComponentBase component)
    {
        Initialize();
        var properties = component.GetType().GetProperties();
        foreach (var property in properties.OfType<IBlossomEntityProxy>())
            await Watch(property);
    }

    public async Task Watch(IBlossomEntityProxy entity)
    {
        await On(entity, (ev) => entity.Update(ev.Changes));
    }

    public async Task Watch(IEnumerable<IBlossomEntityProxy> entities)
    {
        await On(entities, (e, ev) => e.Update(ev.Changes));
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

    public async Task DisposeAsync(ComponentBase component)
    {
        var properties = component.GetType().GetProperties();
        foreach (var property in properties.OfType<IBlossomEntityProxy>())
            await StopWatching(property);
    }

    public async Task GoOnline()
    {
        if (Connection == null)
            Initialize();
        
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

    public async Task GoOffline()
    {
        if (Connection != null)
            await Connection.DisposeAsync();

        foreach (var evt in Events)
            evt.Dispose();

        Events.Clear();
        Subscriptions.Clear();
    }

    public async Task On(IBlossomEntityProxy entity, Action<BlossomEvent> action)
    {
        await GoOnline();
        
        if (!Subscriptions.TryGetValue(entity.SubscriptionId, out int value))
        {
            Subscriptions.Add(entity.SubscriptionId, 1);
            await InvokeAsync("Watch", entity.SubscriptionId);
        }
        else
        {
            Subscriptions[entity.SubscriptionId] = ++value;
        }

        Events.Add(Connection!.On<BlossomEvent>(entity.SubscriptionId, evt =>
        {
            action(evt);
            StateHasChanged();
        }));
    }

    public async Task On(IEnumerable<IBlossomEntityProxy> entities, Action<IBlossomEntityProxy, BlossomEvent> action)
    {
        await GoOnline();
        
        var subscriptionIds = entities.Select(x => x.SubscriptionId);
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

        if (subscriptionIds.Any())
            await InvokeAsync("Watch", [subscriptionIds]);
    }

    private async Task InvokeAsync(string method, params object[] parameters)
    {
        if (IsConnected)
            await Connection!.InvokeAsync(method, parameters);
        else
            Connection!.On("_UserConnected", async () =>
            {
                await Connection!.InvokeAsync(method, parameters);
            });
    }
}
