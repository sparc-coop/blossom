using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Sparc.Blossom.Server.ApiGenerator;

[Generator]
public class BlossomApiRecordGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var specifications = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => Where(s),
                transform: static (ctx, _) => Select(ctx)
        ).Where(static m => m is not null);

        context.RegisterSourceOutput(specifications, static (spc, source) => Generate(source, spc));
    }

    static bool Where(SyntaxNode syntax)
    {
        if (syntax is not ClassDeclarationSyntax classDeclaration)
            return false;

        var baseTypes = new[] { "BlossomEntity", "BlossomQuery" };

        return
            classDeclaration.Modifiers.Any(m => m.Text == "public") &&
            (classDeclaration.BaseList == null ||
            !classDeclaration.BaseList.Types.Any(t => baseTypes.Any(b => t.Type.ToString().Contains(b))));
    }

    static BlossomApiInfo Select(GeneratorSyntaxContext ctx) => new((ClassDeclarationSyntax)ctx.Node);

    static void Generate(BlossomApiInfo source, SourceProductionContext spc)
    {
        var properties = string.Join(", ", source.Properties.Select(x => $"{x.Type} {x.Name}"));
        var ofName = source.OfName == null ? "" : $"<{source.OfName}>";

        var code = new StringBuilder();
        code.Append($$"""
namespace Sparc.Blossom.Api;
{{source.Nullable}}

public record {{source.Name}}{{ofName}}({{properties}});
""");
        
        spc.AddSource($"{source.Namespace}.{source.Name}.g.cs", code.ToString());
    }
}
