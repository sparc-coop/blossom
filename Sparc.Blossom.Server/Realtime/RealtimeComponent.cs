using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Sparc.Blossom.Api;

namespace Sparc.Blossom.Realtime;

public class BlossomRealtime : ComponentBase
{
    [CascadingParameter] public HubConnection? Hub { get; set; }
    readonly List<IDisposable> Events = [];
    protected readonly static Dictionary<string, int> Subscriptions = [];
    protected List<string> LocalSubscriptions = [];

    protected override async Task OnInitializedAsync()
    {
        await SubscribeToBlossomEntityChanges();
    }

    private async Task SubscribeToBlossomEntityChanges()
    {
        var properties = GetType().GetProperties();
        foreach (var property in properties.OfType<IBlossomEntityProxy>())
        {
            var subscriptionId = $"{property.GetType().Name}-{property.GenericId}";
            await On(subscriptionId, (ev) => property.Update(ev.Changes));
        }
    }

    protected async Task On(string subscriptionId, Action<BlossomEvent> action)
    {
        if (Hub == null)
            return;

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

        Events.Add(Hub.On<BlossomEvent>(subscriptionId, evt =>
        {
            action(evt);
            StateHasChanged();
        }));
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