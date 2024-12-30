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
        {
            if (source.IsEntity)
            {
                properties.AppendLine($@"{property.Type} {property.FieldName}{property.PostModifiers(true)}");
                properties.AppendLine($@"{property.Modifiers} {property.Type} {property.Name} {{ get => {property.FieldName}; {property.SetModifiers} set => _set(ref {property.FieldName}, value); }}");
            }
            else
            {
                properties.AppendLine($@"{property.Modifiers} {property.Type} {property.Name} {{ get; set; }}{property.PostModifiers(false)}");
            }
        }

        //properties.AppendLine($@"public required string Id {{ get; set; }}");

        foreach (var constant in source.Constants)
            properties.AppendLine(constant.Body);

        var proxy = source.IsEntity
            ? $" : BlossomEntityProxy<{source.Name}, {source.BaseOfName}>"
            : "";

        if (source.IsEntity)
        {
            foreach (var method in source.Methods)
            {
                var parameterPrefix = method.Arguments.Length > 0 ? ", " : "";
                commands.AppendLine($@"public async Task {method.Name}({method.Arguments}) => await Runner.Execute(Id, ""{method.Name}""{parameterPrefix}{method.Parameters});");
            }
        }

        return $$"""
namespace Sparc.Blossom;
{{source.Nullable}}
        
public partial class {{source.Name}}{{source.OfName}} {{proxy}}
{
    {{properties}}
    {{commands}}
}
""";
    }
}
