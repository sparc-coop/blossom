using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Sparc.Blossom.ApiGenerator;

[Generator]
internal class BlossomApiClassGenerator() : BlossomGenerator<ClassDeclarationSyntax>(Code)
{
    static string Code(BlossomApiInfo source)
    {
        var properties = new StringBuilder();
        var commands = new StringBuilder();

        foreach (var property in source.Properties)
            properties.AppendLine($@"{property.Modifiers} {property.Type} {property.Name} {{ get; {property.SetModifiers} set; }}{property.PostModifiers}");

        //properties.AppendLine($@"public required string Id {{ get; set; }}");

        foreach (var constant in source.Constants)
            properties.AppendLine(constant.Body);

        var proxy = source.IsEntity
            ? $" : BlossomEntityProxy<{source.Name}, {source.BaseOfName}>" 
            : "";

        if (source.IsEntity)
        {
            commands.AppendLine($@"public async Task Add<TItem>(TItem item) => await Runner.Add(Id, item);");
            commands.AppendLine($@"public async Task Remove<TItem>(TItem item) => await Runner.Remove(Id, item);");

            foreach (var method in source.Methods)
            {
                var parameterPrefix = method.Arguments.Length > 0 ? ", " : "";
                commands.AppendLine($@"public async Task {method.Name}({method.Arguments}) => await Runner.Execute(Id, ""{method.Name}""{parameterPrefix}{method.Parameters});");
            }
        }

        return $$"""
namespace Sparc.Blossom.Api;
{{source.Nullable}}
        
public partial class {{source.Name}}{{source.OfName}} {{proxy}}
{
    {{properties}}
    {{commands}}
}
""";
    }
}
