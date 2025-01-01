using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Sparc.Blossom.ApiGenerator;

[Generator]
internal class BlossomEntityProxyGenerator() : BlossomGenerator<ClassDeclarationSyntax>(Code)
{
    static string Code(BlossomApiInfo source)
    {
        var properties = new StringBuilder();
        var commands = new StringBuilder();
        var constructors = new StringBuilder();

        if (source.IsEntity)
        {
            foreach (var constructor in source.Constructors)
            {
                constructors.AppendLine($@"public static async Task<{source.Name}> Create({constructor.Arguments}) => ToProxy(await Repository.AddAsync(new {source.Name}({constructor.Parameters})));");
            }
        }

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

        foreach (var constant in source.Constants)
            properties.AppendLine(constant.Body);

        var proxy = source.IsEntity
            ? $" : BlossomEntityProxy<{source.FullName}>"
            : "";

        if (source.IsEntity)
        {
            foreach (var method in source.Methods)
            {
                commands.AppendLine($@"public async Task {method.Name}({method.Arguments}) => await Repository.ExecuteAsync(GenericId, entity => entity.{method.Name}({method.Parameters});");
            }
        }

        return $$"""
using Sparc.Blossom.Api;
namespace {{source.Namespace}};
{{source.Nullable}}
        
public partial class {{source.ProxyName}}{{source.OfName}} {{proxy}}
{
    {{properties}}
    {{constructors}}
    {{commands}}
}
""";
    }
}
