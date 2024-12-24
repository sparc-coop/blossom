using Ardalis.Specification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Sparc.Blossom.Api;
using System.ComponentModel;
using System.Reflection;

namespace Sparc.Blossom.Realtime;

public class BlossomHubProxy(NavigationManager nav) : IAsyncDisposable
{
    public HubConnection? Connection { get; set; }

    public bool IsActive { get; private set; }
    public bool IsConnected => Connection?.State == HubConnectionState.Connected;
    public bool HasError;

    public EventCallback<HubConnection>? OnConnected;
    public event EventHandler<EventArgs>? Changed;

    readonly Dictionary<string, List<IDisposable>> _subscriptions = [];
    readonly List<object> _broadcastingEntities = [];
    
    public void StateHasChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public void Initialize(bool isActive, EventCallback<HubConnection>? onConnected = null)
    {
        IsActive = isActive;

        Connection ??= new HubConnectionBuilder()
            .WithUrl($"{nav.BaseUri}")
            .ConfigureLogging(x => { x.AddConsole(); x.AddDebug(); })
            //.AddMessagePackProtocol()
            .WithAutomaticReconnect()
            .Build();

        if (onConnected != null)
            OnConnected = onConnected;
    }

    public async Task ConnectAsync()
    {
        if (!IsActive)
            return;

        if (Connection == null && IsActive)
            Initialize(IsActive);

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

    public async Task Watch(ComponentBase component)
    {
        Initialize(true);
        var type = component.GetType();

        var properties = type.GetProperties().OfType<IBlossomEntityProxy>();
        await Watch(properties);

        var stateHasChanged = type.GetMethod("StateHasChanged", BindingFlags.NonPublic | BindingFlags.Instance);
        Changed += (sender, args) => stateHasChanged!.Invoke(component, null);
    }

    public async Task Watch(IBlossomEntityProxy entity) => await Watch([entity]);

    public async Task Watch(IEnumerable<IBlossomEntityProxy> entities)
    {
        await Watch(entities, (entity, ev) => ev.ApplyTo(entity));
    }

    public async Task StopWatching(ComponentBase component)
    {
        var properties = component.GetType().GetProperties();
        foreach (var property in properties.OfType<IBlossomEntityProxy>())
            await StopWatching(property);
    }

    public async Task StopWatching(IBlossomEntityProxy entity)
    {
        var subscriptionId = SubscriptionId(entity);
        var subscriptions = Subscriptions(entity);
        if (subscriptions == null)
            return;

        foreach (var subscription in subscriptions)
        {
            _subscriptions[subscriptionId].Remove(subscription);
            subscription.Dispose();
        }

        if (_subscriptions[subscriptionId].Count == 0)
        {
            _subscriptions.Remove(subscriptionId);
            await InvokeAsync("StopWatching", subscriptionId);
        }
    }

    public async Task Watch(IEnumerable<IBlossomEntityProxy> entities, Action<IBlossomEntityProxy, BlossomEvent> action)
    {
        if (!IsActive)
            return;
        
        await ConnectAsync();
        
        var newSubscriptions = entities.Select(SubscriptionId).Except(_subscriptions.Keys);
        foreach (var subscriptionId in newSubscriptions)
            _subscriptions.Add(subscriptionId, []);

        if (newSubscriptions.Any())
            await InvokeAsync("Watch", newSubscriptions);

        foreach (var entity in entities)
        {
            _subscriptions[SubscriptionId(entity)].Add(Connection!.On<BlossomEvent>(SubscriptionId(entity), evt =>
            {
                action(entity, evt);
                StateHasChanged();
            }));
        }
    }

    public async Task Broadcast(IBlossomEntityProxy entity)
    {
        if (!IsActive || entity is not INotifyPropertyChanged liveEntity)
            return;

        await ConnectAsync();

        liveEntity.PropertyChanged += Patch;

        if (!_broadcastingEntities.Contains(entity.GenericId))
            _broadcastingEntities.Add(entity.GenericId);
    }

    public Task StopBroadcasting(IBlossomEntityProxy entity)
    {
        if (!IsActive || entity is not INotifyPropertyChanged liveEntity)
            return Task.CompletedTask;

        liveEntity.PropertyChanged -= Patch;

        if (_broadcastingEntities.Contains(entity.GenericId))
            _broadcastingEntities.Remove(entity.GenericId);

        return Task.CompletedTask;
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

    private void Patch(object? sender, PropertyChangedEventArgs e)
    {
        var patch = (e as BlossomPropertyChangedEventArgs)?.Patch;
        if (sender is not IBlossomEntityProxy entity || patch == null)
            return;

        if (!_broadcastingEntities.Contains(entity.GenericId))
            return;

        _ = entity.GenericRunner.Patch(entity.GenericId, patch);
    }

    public async ValueTask DisposeAsync()
    {
        if (Connection != null)
            await Connection.DisposeAsync();

        foreach (var subscription in _subscriptions.SelectMany(x => x.Value))
            subscription.Dispose();

        _subscriptions.Clear();
    }
    
    public static string SubscriptionId(IBlossomEntityProxy entity) => $"{entity.GetType().Name}-{entity.GenericId}";
    List<IDisposable>? Subscriptions(IBlossomEntityProxy entity) => _subscriptions.ContainsKey(SubscriptionId(entity)) 
        ? _subscriptions[SubscriptionId(entity)].Where(x => x == entity).ToList() 
        : null;
}
