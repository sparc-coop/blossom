
using System.Reflection;
using System.Text.RegularExpressions;

namespace Sparc.Blossom;

public class BlossomMethod(MethodInfo method)
{
    public string Name { get; } = method.Name;
    public string FriendlyName =>
        Regex.Replace(Name, @"(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", " $1");


    public string? EditorType => "";

    public List<BlossomProperty> Parameters { get; } = method
        .GetParameters()
        .Select(x => new BlossomProperty(x)).ToList();

    public async Task InvokeAsync(object entity)
    {
        if (entity is not IBlossomEntityProxy proxy)
            throw new InvalidOperationException("Entity must be a proxy");

        var parameters = Parameters.Select(x => x.Value(entity)).ToArray();
        var task = (Task)method.Invoke(proxy, parameters)!;
        await task;

        foreach (var property in Parameters)
            property.SetValue(entity, null);
    }
}
