using System.Collections;
using System.Reflection;
using System.Text.Json;

namespace Sparc.Blossom.Realtime;

public class BlossomPatch(string op, string path, object? value)
{
    public string Op { get; private set; } = op;
    public string Path { get; private set; } = path;
    public object? Value { get; private set; } = value;

    public static List<BlossomPatch> Create(object original, object updated)
    {
        var patches = new List<BlossomPatch>();
        DeepCompare(original, updated, patches);
        return patches;
    }

    static void DeepCompare(object original, object updated, List<BlossomPatch> patches, string currentPath = "/")
    {
        if (!original.GetType().IsClass && !original.Equals(updated))
        {
            var op = original == null ? "add" : updated == null ? "remove" : "replace";
            patches.Add(new BlossomPatch(op, currentPath, updated));
        }
        else if (original is IEnumerable enumerable)
        {
            var originalList = JsonSerializer.Serialize(original);
            var updatedList = JsonSerializer.Serialize(updated);
            if (originalList != updatedList)
            {
                patches.Add(new BlossomPatch("replace", currentPath, updated));
            }
        }
        else
        {
            var propertiesToCompare = original.GetType().GetRuntimeProperties()
                .Where(x => x.CanRead && x.CanWrite && (x.SetMethod?.IsPublic == true || x.SetMethod?.IsAssembly == true));

            foreach (var property in propertiesToCompare)
            {
                var updatedProperty = updated.GetType().GetRuntimeProperty(property.Name);
                if (updatedProperty == null)
                    continue;

                var originalValue = property.GetValue(original);
                var updatedValue = updatedProperty.GetValue(updated);
                var path = currentPath + "/" + property.Name;
                DeepCompare(originalValue, updatedValue, patches, path);
            }
        }
    }

    public T ApplyTo<T>(T original) where T : class
    {
        var path = Path.Split('/');
        object current = original;
        for (var i = 1; i < path.Length; i++)
        {
            var property = current.GetType().GetProperty(path[i]);
            if (i == path.Length - 1)
            {
                property.SetValue(current, Value);
            }
            else
            {
                current = property.GetValue(current);
            }
        }
        return original;
    }
}
