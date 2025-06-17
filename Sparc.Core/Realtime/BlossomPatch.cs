using System.ComponentModel;
using SystemTextJsonPatch;

namespace Sparc.Blossom;

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

    public BlossomPatch(object previousEntity, object currentEntity, List<string>? propertiesToSelect = null, bool ignoreNulls = false) : this()
    {
        var properties = previousEntity.GetType().GetProperties();
        
        if (propertiesToSelect != null)
            properties = properties.Where(x => propertiesToSelect.Any(y => y == x.Name)).ToArray();

        foreach (var property in properties)
        {
            var currentEntityProperty = currentEntity.GetType().GetProperty(property.Name);
            if (currentEntityProperty == null)
                continue;

            From(property.Name, property.GetValue(previousEntity), currentEntityProperty.GetValue(currentEntity), ignoreNulls);
        }
    }

    public void ApplyTo<T>(T target)
    {
        if (target == null)
            return;

        try
        {
            JsonPatchDocument.ApplyTo(target);
        }
        catch
        {
        }
    }

    public BlossomPatch Combine(BlossomPatch patch)
    {
        foreach (var operation in patch.JsonPatchDocument.Operations)
            JsonPatchDocument.Operations.Add(operation);

        return this;
    }

    public BlossomPatch? From<TField>(string propertyName, TField? previousValue, TField? value, bool ignoreNulls = false)
    {
        var path = $"/{propertyName}";

        if (previousValue == null)
        {
            if (value == null)
                return this;
            JsonPatchDocument.Add(path, value);
        }
        else if (value == null)
        {
            if (ignoreNulls)
                return this;

            JsonPatchDocument.Remove(path);
        }
        else if (EqualityComparer<TField>.Default.Equals(previousValue, value))
        {
            return this;
        }
        else
        {
            JsonPatchDocument.Replace(path, value);
        }

        return this;
    }
}
