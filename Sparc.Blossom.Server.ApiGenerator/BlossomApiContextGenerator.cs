using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace Sparc.Blossom.Server.ApiGenerator;

[Generator]
public class BlossomApiContextGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var specifications = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => Where(s, "Specification"),
                transform: static (ctx, _) => Select(ctx)
        ).Where(static m => m is not null);

        context.RegisterSourceOutput(specifications, static (spc, source) => Generate(source, spc));
    }

    static bool Where(SyntaxNode syntax, string baseType)
    {
        if (syntax is not ClassDeclarationSyntax classDeclaration)
            return false;

        return
            classDeclaration.BaseList != null &&
            classDeclaration.BaseList.Types.Any(t => t.Type.ToString().Contains(baseType));
    }

    static BlossomApiInfo Select(GeneratorSyntaxContext ctx) => new((ClassDeclarationSyntax)ctx.Node);

    static void Generate(BlossomApiInfo source, SourceProductionContext spc)
    {
        var queries = new StringBuilder();
        foreach (var method in source.Methods)
        {
            var parameterPrefix = method.Parameters.Length > 0 ? ", " : "";
            queries.AppendLine($@"public async Task<IEnumerable<{source.Name}>> {source.Name}({method.Parameters}) => await Runner.QueryAsync(""{source.Name}""{parameterPrefix}{method.Parameters});");
        }

        var records = new StringBuilder();
        var properties = string.Join(", ", source.Properties.Select(x => $"{x.Type} {x.Name}"));
        records.AppendLine($@"public record {source.Name}({properties});");

        var code = new StringBuilder();
        code.Append($$"""
namespace {{source.Namespace}}.Client
{
    {{records}}
    public partial class {{source.BasePluralName}} : BlossomApiContext<{{source.BaseName}}>
    {
        {{queries}}
    }
}
""");
        
        spc.AddSource($"{source.BaseName}.{source.Name}.g.cs", code.ToString());
    }
}
