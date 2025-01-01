using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace Sparc.Blossom;

public interface IBlossomEntityProxy : INotifyPropertyChanged
{
    object GenericId { get; }
}

public interface IBlossomEntityProxy<T>
{
}

public class BlossomEntityProxy<T>(T entity, IRepository<T> repository) 
    : BlossomProxy<T>(repository), IBlossomEntityProxy<T>, IBlossomEntityProxy
    where T : BlossomEntity
{
    public object GenericId { get; } = entity.GenericId!;

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

    public async Task<BlossomEntityProxy<T>> Create(object[] parameters)
    {
        var entity = Activator.CreateInstance(typeof(T), parameters) as T;
        if (entity == null)
            throw new Exception("Failed to create entity.");

        return new BlossomEntityProxy<T>(entity, Repository);
    }

    public async Task Patch(BlossomPatch changes)
    {
        var entity = await Repository.FindAsync(GenericId);
        if (entity == null)
            return;

        changes.ApplyTo(entity);
        //await Events.BroadcastAsync(new BlossomEntityPatched<T>(entity, changes));
        await Repository.UpdateAsync(entity);
    }

    public async Task Delete(object id)
    {
        var entity = await Repository.FindAsync(id);
        if (entity == null)
            return;
        await Repository.DeleteAsync(entity);
    }

    public override int GetHashCode() => GenericId.GetHashCode();
    public override bool Equals(object obj) => obj is IBlossomEntityProxy other && GenericId.Equals(other.GenericId);
}