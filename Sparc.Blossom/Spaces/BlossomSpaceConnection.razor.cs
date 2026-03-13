using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Sparc.Blossom.Spaces;

public class BlossomSpaceConnectionProvider(string baseUri)
{
    public HubConnection CreateConnection()
    {
        return new HubConnectionBuilder()
            .WithUrl($"{baseUri}/_realtime")
            .WithStatefulReconnect()
            //.AddMessagePackProtocol()
            .WithAutomaticReconnect()
            .Build();
    }
}

public partial class BlossomSpaceConnection : IAsyncDisposable
{
    [Parameter] public RenderFragment ChildContent { get; set; } = null!;
    [Parameter] public required BlossomSpace Space { get; set; }
    [Parameter] public EventCallback<HubConnection> OnConnected { get; set; } = default;
    [Inject] public required BlossomSpaceConnectionProvider ConnectionProvider { get; set; }
    public HubConnection? Connection { get; set; }
    public bool IsActive { get; private set; }
    public bool IsConnected => Connection?.State == HubConnectionState.Connected;
    public bool HasError;

    public event EventHandler<EventArgs>? Changed;
    readonly Dictionary<string, List<IDisposable>> _subscriptions = [];
    readonly List<object> _broadcastingEntities = [];

    public void NotifyStateChanged() => Changed?.Invoke(this, EventArgs.Empty);


    protected override void OnInitialized()
    {
        Connection ??= ConnectionProvider.CreateConnection();
    }

    async Task ConnectAsync()
    {
        if (!IsActive || Connection?.State != HubConnectionState.Disconnected)
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
                if (OnConnected.HasDelegate)
                    await OnConnected.InvokeAsync(Connection);

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