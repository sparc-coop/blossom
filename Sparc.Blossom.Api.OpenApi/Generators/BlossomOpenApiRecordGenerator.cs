using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.ApiGenerator;

[Generator]
internal class BlossomOpenApiRecordGenerator() : BlossomGenerator<RecordDeclarationSyntax>(Code)
{
    static string Code(BlossomApiInfo source)
    {
        var properties = string.Join(", ", source.Properties.Select(x => $"{x.Type} {x.Name}"));

        return $$"""
namespace Sparc.Blossom.Api;
{{source.Nullable}}

public record {{source.Name}}{{source.OfName}}({{properties}});
""";
    }
}
