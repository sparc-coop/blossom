using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.JSInterop;

namespace Sparc.Blossom;

public sealed class BrowserTimeProvider(IJSRuntime js) : TimeProvider
{
    private TimeZoneInfo? _browserLocalTimeZone;

    // Notify when the local time zone changes
    public event EventHandler? LocalTimeZoneChanged;

    public override TimeZoneInfo LocalTimeZone
        => _browserLocalTimeZone ?? base.LocalTimeZone;

    internal bool IsLocalTimeZoneSet => _browserLocalTimeZone != null;

    public IJSRuntime Js { get; } = js;

    // Set the local time zone
    public async Task InitializeAsync()
    {
        if (!IsLocalTimeZoneSet)
        {
            try
            {
                var timeZone = await Js.InvokeAsync<string>("eval", "Intl.DateTimeFormat().resolvedOptions().timeZone");
                if (!TimeZoneInfo.TryFindSystemTimeZoneById(timeZone, out var timeZoneInfo))
                {
                    timeZoneInfo = null;
                }

                if (timeZoneInfo != LocalTimeZone)
                {
                    _browserLocalTimeZone = timeZoneInfo;
                    LocalTimeZoneChanged?.Invoke(this, EventArgs.Empty);
                }
            }
            catch (JSDisconnectedException)
            {
            }
        }
    }
}

public static class TimeProviderExtensions
{
    public static DateTime ToLocalDateTime(this TimeProvider timeProvider, DateTime dateTime)
    {
        return dateTime.Kind switch
        {
            DateTimeKind.Unspecified => throw new InvalidOperationException("Unable to convert unspecified DateTime to local time"),
            DateTimeKind.Local => dateTime,
            _ => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(dateTime, timeProvider.LocalTimeZone), DateTimeKind.Local),
        };
    }

    public static DateTime ToLocalDateTime(this TimeProvider timeProvider, DateTimeOffset dateTime)
    {
        var local = TimeZoneInfo.ConvertTimeFromUtc(dateTime.UtcDateTime, timeProvider.LocalTimeZone);
        local = DateTime.SpecifyKind(local, DateTimeKind.Local);
        return local;
    }
}

public sealed class LocalTime : ComponentBase, IDisposable
{
    [Inject]
    public TimeProvider TimeProvider { get; set; } = default!;

    [Parameter]
    public DateTime? DateTime { get; set; }

    protected override void OnInitialized()
    {
        if (TimeProvider is BrowserTimeProvider browserTimeProvider)
        {
            browserTimeProvider.LocalTimeZoneChanged += LocalTimeZoneChanged;
        }
    }

    protected override void BuildRenderTree(RenderTreeBuilder builder)
    {
        if (DateTime != null)
        {
            builder.AddContent(0, TimeProvider.ToLocalDateTime(DateTime.Value));
        }
    }

    public void Dispose()
    {
        if (TimeProvider is BrowserTimeProvider browserTimeProvider)
        {
            browserTimeProvider.LocalTimeZoneChanged -= LocalTimeZoneChanged;
        }
    }

    private void LocalTimeZoneChanged(object? sender, EventArgs e)
    {
        _ = InvokeAsync(StateHasChanged);
    }
}