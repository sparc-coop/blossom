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
                predicate: static (s, _) => Where(s, "BlossomQuery"),
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
        var usings = string.Join("\n", source.Usings);
        
        var records = new StringBuilder();
        var properties = string.Join(", ", source.Properties.Select(x => $"{x.Type} {x.Name}"));
        if (properties.Length > 0)
            records.AppendLine($@"public record {source.Name}({properties});");

        var queries = new StringBuilder();
        foreach (var constructor in source.Constructors)
        {
            var parameterPrefix = constructor.Arguments.Length > 0 ? ", " : "";
            var returnType = properties.Length > 0 ? source.Name : source.BaseName;
            queries.AppendLine($@"public async Task<IEnumerable<{returnType}>> {source.Name}({constructor.Arguments}) => await Runner.QueryAsync(""{source.Name}""{parameterPrefix}{constructor.Parameters});");
        }

        var code = new StringBuilder();
        code.Append($$"""
{{usings}}
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
