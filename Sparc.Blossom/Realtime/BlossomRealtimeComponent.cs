using Microsoft.AspNetCore.Components;

namespace Sparc.Blossom;

public class BlossomRealtimeComponent : ComponentBase, IAsyncDisposable
{
    [Inject]
    public required BlossomHubProxy HubProxy { get; set; }

    protected override async Task OnInitializedAsync()
    {
        await HubProxy.Watch(this);
    }

    public async ValueTask DisposeAsync()
    {
        await HubProxy.DisposeAsync();
    }

}
