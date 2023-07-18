using Microsoft.JSInterop;

namespace Sparc.Blossom.Realtime;

public class Bodies
{
    public record Body(string name, string type, bool animate, string? fill, string? stroke);
    public record Rectangle(string name, int x, int y, int width, int height, bool animate = true, string? fill = null, string? stroke = null) 
        : Body(name, "rectangle", animate, fill, stroke);
}

public record Animation(string Id, int Height, int Width);
public class BlossomAnimator : IAsyncDisposable
{
    private readonly Lazy<Task<IJSObjectReference>> Js;
    readonly List<Bodies.Body> Bodies = new();
    bool IsStarted = false;

    public string Id { get; private set; } = "";

    public BlossomAnimator(IJSRuntime js)
    {
        Js = new(() => js.InvokeAsync<IJSObjectReference>(
                       "import", "./_content/Sparc.Blossom.Client/Realtime/BlossomAnimation.razor.js").AsTask());
    }

    public async Task<Animation> InitializeAsync(string id)
    {
        Id = id;
        var js = await Js.Value;
        return await js.InvokeAsync<Animation>("initialize", id);
    }

    public async Task AddAsync(Bodies.Body body)
    {
        var js = await Js.Value;
        await js.InvokeVoidAsync("add", body);
        Bodies.Add(body);
    }

    public async Task StartAsync()
    {
        if (IsStarted)
            return;

        IsStarted = true;
        var js = await Js.Value;
        await js.InvokeVoidAsync("animate");
    }

    public async Task AnimateAsync()
    {
        foreach (var body in Bodies.Where(x => x.animate))
        {
            await AnimateAsync(body);
        }
    }

    public async Task AnimateAsync(Bodies.Body body)
    {
        var js = await Js.Value;
        await js.InvokeVoidAsync("animatebody", body);
    }

    public async ValueTask DisposeAsync()
    {
        if (Js.IsValueCreated)
        {
            var js = await Js.Value;
            await js.DisposeAsync();
        }
    }
}
