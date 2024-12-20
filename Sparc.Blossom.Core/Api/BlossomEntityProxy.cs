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

    public List<BlossomPatch> Changes { get; private set; } = [];
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
        if (!Changes.Any())
            return;

        foreach (var change in Changes.Where(x => !x.IsLocked).ToList())
        {
            change.IsLocked = true;
            await Runner.Patch(Id!, change);
            Changes.Remove(change);
        }
    }

    protected void Patch<TField>(ref TField field, TField value, [CallerMemberName] string propertyName = "")
    {
        if (_isSyncingFromBlossom || !IsLive)
        {
            field = value;
            return;
        }

        var change = CurrentChange().From(propertyName, field, value);
        if (change != null)
        {
            Patch(change);
            _ = SyncToBlossom();
        }
    }

    private BlossomPatch CurrentChange()
    {
        var change = Changes.FirstOrDefault(x => !x.IsLocked);
        if (change == null)
        {
            change = new BlossomPatch();
            Changes.Add(change);
        }

        return change;
    }
}