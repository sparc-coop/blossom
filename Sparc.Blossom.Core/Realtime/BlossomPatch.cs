using Sparc.Blossom.Data;
using System.ComponentModel;
using SystemTextJsonPatch;

namespace Sparc.Blossom.Realtime;

public class BlossomPropertyChangedEventArgs(string propertyName, BlossomPatch patch) : PropertyChangedEventArgs(propertyName)
{
    public BlossomPatch Patch { get; private set; } = patch;
}

public record BlossomPatch
{
    public JsonPatchDocument JsonPatchDocument { get; set; }
    public bool IsLocked { get; internal set; }

    public BlossomPatch()
    {
        JsonPatchDocument = new();
    }
    
    public BlossomPatch(BlossomEntity previousEntity, BlossomEntity currentEntity) : this()
    {
        var properties = previousEntity.GetType().GetProperties();
        foreach (var property in properties)
            From(property.Name, property.GetValue(previousEntity), property.GetValue(currentEntity));
    }

    public void ApplyTo<T>(T target)
    {
        if (target != null)
            JsonPatchDocument.ApplyTo(target);
    }

    public BlossomPatch Combine(BlossomPatch patch)
    {
        foreach (var operation in patch.JsonPatchDocument.Operations)
            JsonPatchDocument.Operations.Add(operation);
        
        return this;
    }

    public static BlossomPatch? From<TField>(string propertyName, TField? previousValue, TField? value)
    {
        var patch = new BlossomPatch();

        var path = $"/{propertyName}";

        if (previousValue == null)
        {
            if (value == null)
                return null;
            patch.JsonPatchDocument.Add(path, value);
        }
        else if (value == null)
        {
            patch.JsonPatchDocument.Remove(path);
        }
        else if (EqualityComparer<TField>.Default.Equals(previousValue, value))
        {
            return null;
        }
        else
        {
            patch.JsonPatchDocument.Replace(path, value);
        }

        return patch;
    }
}
