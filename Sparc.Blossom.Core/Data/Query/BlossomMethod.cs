
using System.Reflection;
using System.Text.RegularExpressions;

namespace Sparc.Blossom.Api;

public class BlossomMethod(MethodInfo method)
{
    public string Name { get; } = method.Name;
    public string FriendlyName =>
        Regex.Replace(Name, @"(?<!^)([A-Z][a-z]|(?<=[a-z])[A-Z])", " $1");


    public string? EditorType => "";

    public List<BlossomProperty> Parameters { get; } = method
        .GetParameters()
        .Select(x => new BlossomProperty(x)).ToList();
}
