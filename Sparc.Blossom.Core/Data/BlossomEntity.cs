using Newtonsoft.Json;
using System.Reflection;

namespace Sparc.Blossom;

public abstract class BlossomEntity
{
    public virtual object GenericId { get; } = null!;
    protected List<BlossomEvent>? _events;

    // Event system
    public List<BlossomEvent> Publish()
    {
        _events ??= [];

        var domainEvents = _events.ToList();
        _events.Clear();

        return domainEvents;
    }

    protected void Broadcast<T>() where T : BlossomEvent => Broadcast((T)Activator.CreateInstance(typeof(T), this));

    protected void Broadcast(BlossomEvent notification)
    {
        _events ??= [];
        _events!.Add(notification);
    }

    public void Add<T>(T relationship)
    {
        if (typeof(T).IsPrimitive || typeof(T) == typeof(string))
            throw new Exception($"Relationship {typeof(T).Name} on entity {GetType().Name} not found.");

        var collectionProperty = GetCollectionProperty<T>();
        if (collectionProperty != null)
        {
            var collection = (ICollection<T>)collectionProperty.GetValue(this);
            collection.Add(relationship);
            return;
        }

        var singleProperty = GetSingleProperty<T>();
        if (singleProperty != null)
        {
            singleProperty.SetValue(this, relationship);
            return;
        }

        throw new Exception($"Relationship {typeof(T).Name} on entity {GetType().Name} not found.");
    }

    public void Remove<T>(T relationship)
    {
        var collectionProperty = GetCollectionProperty<T>();
        if (collectionProperty != null)
        {
            var collection = (ICollection<T>)collectionProperty.GetValue(this);
            collection.Remove(relationship);
            return;
        }

        var singleProperty = GetSingleProperty<T>();
        if (singleProperty != null)
        {
            singleProperty.SetValue(this, null);
            return;
        }

        throw new Exception($"Relationship {typeof(T).Name} on entity {GetType().Name} not found.");
    }

    PropertyInfo? GetCollectionProperty<T>() =>
    GetType().GetProperties().FirstOrDefault(x => typeof(ICollection<T>).IsAssignableFrom(x.PropertyType));

    PropertyInfo? GetSingleProperty<T>() =>
        GetType().GetProperties().FirstOrDefault(x => typeof(T).IsAssignableFrom(x.PropertyType));
}

public class BlossomEntity<T> : BlossomEntity where T : notnull
{
    public BlossomEntity()
    {
        Id = default!;
    }

    public BlossomEntity(T id) => Id = id;
    public override object GenericId => Id;

    [JsonProperty("id")]
    public virtual T Id { get; set; }
}
