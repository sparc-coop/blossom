using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Sparc.Blossom;

public class BlossomEntityProxy<T, TId> : IBlossomEntityProxy<T>, IBlossomEntityProxy
{
    public TId Id { get; set; } = default!;
    public object GenericId => Id!;

    public IRunner<T> Runner { get; set; } = null!;
    public IRunner GenericRunner => Runner;

    public event PropertyChangedEventHandler? PropertyChanged;
    protected void OnPropertyChanged<TField>(string propertyName, TField currentValue, TField newValue)
    {
        var patch = BlossomPatch.From(propertyName, currentValue, newValue);
        if (patch != null)
            PropertyChanged?.Invoke(this, new BlossomPropertyChangedEventArgs(propertyName, patch));
    }

    protected bool _set<TField>(ref TField currentValue, TField newValue, [CallerMemberName] string propertyName = "")
    {
        if (EqualityComparer<TField>.Default.Equals(currentValue, newValue)) return false;

        currentValue = newValue;
        OnPropertyChanged(propertyName, currentValue, newValue);
        return true;
    }

    public override int GetHashCode() => GenericId.GetHashCode();
    public override bool Equals(object obj) => obj is IBlossomEntityProxy other && GenericId.Equals(other.GenericId);
}