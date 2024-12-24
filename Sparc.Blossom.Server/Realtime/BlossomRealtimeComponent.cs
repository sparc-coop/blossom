using Microsoft.AspNetCore.Components;

namespace Sparc.Blossom.Realtime;

public class BlossomRealtimeComponent : ComponentBase, IAsyncDisposable
{
    [Inject]
    public BlossomHubProxy HubProxy { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await HubProxy.Watch(this);
    }

    public async ValueTask DisposeAsync()
    {
        await HubProxy.DisposeAsync();
    }

}
