using Ardalis.Specification;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Sparc.Blossom.Realtime;
using System.ComponentModel;
using System.Reflection;
using System.Threading.Channels;

namespace Sparc.Blossom;

public class BlossomSignalRProxy(NavigationManager nav) : BlossomLocalRealtimeProxy
{
    HubConnection? Connection { get; set; }
    public EventCallback<HubConnection>? OnConnected;

    public override ConnectionStates ConnectionState => Connection?.State switch
    {
        HubConnectionState.Disconnected => ConnectionStates.Disconnected,
        HubConnectionState.Connecting => ConnectionStates.Connecting,
        HubConnectionState.Connected => ConnectionStates.Connected,
        _ => ConnectionStates.Disconnected
    };

    public override void Initialize(bool isActive)
    {
        base.Initialize(isActive);

        Connection ??= new HubConnectionBuilder()
            .WithUrl($"{nav.BaseUri}_realtime")
            //.AddMessagePackProtocol()
            .WithAutomaticReconnect()
            .Build();
    }

    public override async Task ConnectAsync()
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

    

    public override async Task Watch(IEnumerable<IBlossomEntityProxy> entities, Action<IBlossomEntityProxy, BlossomEvent> action)
    {
        var newSubscriptions = await base.Watch(entities, action);

        if (newSubscriptions?.Any() == true)
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

    public override async ValueTask DisposeAsync()
    {
        await base.DisposeAsync();

        if (Connection != null)
            await Connection.DisposeAsync();
    }
    
}
