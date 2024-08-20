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
        foreach (var property in source.Properties)
            properties.AppendLine($@"{property.Modifiers} {property.Type} {property.Name} {{ get; set; }}");

        return $$"""
namespace Sparc.Blossom.Api;
{{source.Nullable}}
        
public partial class {{source.Name}}{{source.OfName}}
{
    {{properties}}
}
""";
    }
}
