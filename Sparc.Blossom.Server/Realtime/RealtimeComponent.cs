using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace Sparc.Blossom.Realtime;

public class BlossomRealtime : ComponentBase
{
    [CascadingParameter] public HubConnection? Hub { get; set; }
    readonly List<IDisposable> Events = [];
    protected readonly static Dictionary<string, int> Subscriptions = [];
    protected List<string> LocalSubscriptions = [];

    protected void On<T>(Action<T> action)
    {
        if (Hub != null)
            Events.Add(Hub.On<T>(typeof(T).Name, evt =>
            {
                action(evt);
                StateHasChanged();
            }));
    }

    protected async Task RawOn<String>(string subscriptionId, Action<String> action) 
    {
        if (Hub != null)
        {
            Hub.On<String>(subscriptionId, async evt =>
            {
                action.Invoke(evt);
            });
        }
    }

    protected async Task On<T>(string subscriptionId, Action<T> action) where T : BlossomEvent
    {
        if (Hub != null)
        {
            if (!Subscriptions.TryGetValue(subscriptionId, out int value))
            {
                Subscriptions.Add(subscriptionId, 1);
                if (Hub.State == HubConnectionState.Connected)
                {
                    await Hub!.InvokeAsync("Watch", subscriptionId);
                }
                else
                    Hub.On("_UserConnected", async () =>
                    {
                        await Hub!.InvokeAsync("Watch", subscriptionId);
                    });
            }
            else
            {
                Subscriptions[subscriptionId] = ++value;
            }

            LocalSubscriptions.Add(subscriptionId);

            Events.Add(Hub.On<T>(typeof(T).Name, evt =>
            {

                var equals = evt.SubscriptionId == subscriptionId;
                if (equals)
                {
                    action(evt);
                    StateHasChanged();
                }
            }));
        }
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var subscription in LocalSubscriptions.ToList())
        {
            if (Subscriptions.TryGetValue(subscription, out int value))
            {
                Subscriptions[subscription] = --value;
                if (value <= 0)
                {
                    Subscriptions.Remove(subscription);
                    if (Hub?.State == HubConnectionState.Connected)
                        await Hub!.InvokeAsync("StopWatching", subscription);
                }
            }

            foreach (var evt in Events)
                evt.Dispose();

            Events.Clear();
            LocalSubscriptions.Clear();
        }
    }
}