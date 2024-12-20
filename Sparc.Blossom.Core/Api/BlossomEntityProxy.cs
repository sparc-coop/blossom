using Sparc.Blossom.Realtime;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Api;

public interface IBlossomEntityProxy
{
    string SubscriptionId { get; }
    bool IsLive { get; set; }
    void Patch(BlossomPatch patch);
}

public interface IBlossomEntityProxy<T>
{
    IRunner<T> Runner { get; set; }
}

public class BlossomEntityProxy<T, TId> : IBlossomEntityProxy<T>, IBlossomEntityProxy
{
    public TId Id { get; set; } = default!;
    public string SubscriptionId => $"{GetType().Name}-{Id}";
    public bool IsLive { get; set; }

    public BlossomPatch? Changes { get; private set; }
    public IRunner<T> Runner { get; set; } = null!;

    bool _isSyncingFromBlossom;
    public void Patch(BlossomPatch patch)
    {
        _isSyncingFromBlossom = true;
        patch.ApplyTo(this);
        _isSyncingFromBlossom = false;
    }

    private async Task SyncToBlossom()
    {
        if (Changes == null)
            return;

        var changes = Clone(Changes);
        Changes = null;
        await Runner.Patch(Id!, changes);
    }

    protected void Patch<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = "")
    {
        if (_isSyncingFromBlossom || !IsLive)
        {
            field = value;
            return;
        }
        
        Changes ??= new BlossomPatch();
        var patch = Changes.From(propertyName, field, value);
        if (patch != null)
        {
            patch.ApplyTo(this);
            _ = SyncToBlossom();
        }
    }

    static readonly JsonSerializerOptions CloneOptions = new()
    {
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        WriteIndented = false,
        Converters = { new JsonStringEnumConverter() },
        IncludeFields = true
    };

    private static TItem Clone<TItem>(TItem item)
    {
        return JsonSerializer.Deserialize<TItem>(JsonSerializer.Serialize(item, CloneOptions))!;
    }
}