using Ardalis.Specification;
using MediatR;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Sparc.Blossom.Realtime;
using System.Reflection;

namespace Sparc.Blossom;

public class BlossomLocalRealtimeProxy : IRealtimeProxy, INotificationHandler<BlossomEvent>
{
    public bool IsActive { get; protected set; }
    public bool IsConnected => ConnectionState == ConnectionStates.Connected;
    public bool HasError;
    public virtual ConnectionStates ConnectionState => ConnectionStates.Connected;

    protected readonly Dictionary<string, List<IDisposable>> _subscriptions = [];
    protected readonly List<object> _broadcastingEntities = [];
    public event EventHandler<EventArgs>? Changed;
    public void StateHasChanged() => Changed?.Invoke(this, EventArgs.Empty);

    public virtual void Initialize(bool isActive)
    {
        IsActive = isActive;
    }

    public virtual Task ConnectAsync() => Task.CompletedTask;

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

    public virtual async Task<IEnumerable<string>?> Watch(IEnumerable<IBlossomEntityProxy> entities, Action<IBlossomEntityProxy, BlossomEvent> action)
    {
        if (!IsActive)
            return null;

        await ConnectAsync();

        var newSubscriptions = entities.Select(SubscriptionId).Except(_subscriptions.Keys);
        foreach (var subscriptionId in newSubscriptions)
            _subscriptions.Add(subscriptionId, []);

        foreach (var entity in entities)
        {
            _subscriptions[SubscriptionId(entity)].Add(entity!.On<BlossomEvent>(evt =>
            {
                action(entity, evt);
                StateHasChanged();
            }));
        }

        return newSubscriptions;
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

    public static string SubscriptionId(IBlossomEntityProxy entity) => $"{entity.GetType().Name}-{entity.GenericId}";
    List<IDisposable>? Subscriptions(IBlossomEntityProxy entity) => _subscriptions.ContainsKey(SubscriptionId(entity))
        ? _subscriptions[SubscriptionId(entity)].Where(x => x == entity).ToList()
        : null;

    public virtual async ValueTask DisposeAsync()
    {
        foreach (var subscription in _subscriptions.SelectMany(x => x.Value))
            subscription.Dispose();

        _subscriptions.Clear();

        await Task.CompletedTask;
    }

    public Task Handle(BlossomEvent notification, CancellationToken cancellationToken)
    {
        throw new NotImplementedException();
    }
}
