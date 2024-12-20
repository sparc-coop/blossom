using SystemTextJsonPatch;

namespace Sparc.Blossom.Realtime;

public record BlossomPatch
{
    public JsonPatchDocument JsonPatchDocument { get; set; }
    public bool IsLocked { get; internal set; }

    public BlossomPatch()
    {
        JsonPatchDocument = new();
    }
    
    public BlossomPatch(object previousEntity, object currentEntity) : this()
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

    public BlossomPatch Add(string path, object value)
    {
        JsonPatchDocument.Add(path, value);
        return this;
    }

    public BlossomPatch Replace(string path, object value)
    {
        JsonPatchDocument.Replace(path, value);
        return this;
    }

    public BlossomPatch Remove(string path)
    {
        JsonPatchDocument.Remove(path);
        return this;
    }

    public BlossomPatch Move(string from, string path)
    {
        JsonPatchDocument.Move(from, path);
        return this;
    }

    public BlossomPatch Copy(string from, string path)
    {
        JsonPatchDocument.Copy(from, path);
        return this;
    }

    public BlossomPatch Test(string path, object value)
    {
        JsonPatchDocument.Test(path, value);
        return this;
    }

    public BlossomPatch? From<TField>(string propertyName, TField? previousValue, TField? value)
    {
        var path = $"/{propertyName}";

        if (previousValue == null)
        {
            if (value == null)
                return null;
            Add(path, value);
        }
        else if (value == null)
        {
            Remove(path);
        }
        else if (EqualityComparer<TField>.Default.Equals(previousValue, value))
        {
            return null;
        }
        else
        {
            Replace(path, value);
        }

        return this;
    }
}
