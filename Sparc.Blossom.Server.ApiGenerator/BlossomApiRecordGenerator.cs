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
        if (syntax is RecordDeclarationSyntax recordDeclaration && recordDeclaration.BaseList?.Types.Any(x => x.Type.ToString().Contains("BlossomRecord")) == true)
            return true;
        
        if (syntax is not ClassDeclarationSyntax classDeclaration)
            return false;

        var baseTypesToExclude = new[] { "BlossomEntity", "BlossomQuery" };

        return
            classDeclaration.Modifiers.Any(m => m.Text == "public") &&
            (classDeclaration.BaseList == null ||
            !classDeclaration.BaseList.Types.Any(t => baseTypesToExclude.Any(b => t.Type.ToString().Contains(b))));
    }

    static BlossomApiInfo Select(GeneratorSyntaxContext ctx) => new((TypeDeclarationSyntax)ctx.Node);

    static void Generate(BlossomApiInfo source, SourceProductionContext spc)
    {
        if (source.Properties == null)
            throw new Exception("WHYYY" + source.Properties);
        
        var isBlossomRecord = source.BaseName == "BlossomRecord";
        var properties = string.Join(", ", source.Properties.Select(x => $"{x.Type} {x.Name}"));
        var ofName = source.OfName == null ? "" : $"<{source.OfName}>";

        var proxy = isBlossomRecord ? $" : BlossomRecordProxy<{source.Name}>" : "";

        var code = new StringBuilder();
        code.AppendLine($$"""
namespace Sparc.Blossom.Api;
{{source.Nullable}}

public record {{source.Name}}{{ofName}}({{properties}}){{proxy}};
""");

        if (isBlossomRecord)
            code.AppendLine($$"""
public partial class {{source.PluralName}} : BlossomApiContext<{{source.Name}}>
{
    public {{source.PluralName}}(IRunner<{{source.Name}}> runner) : base(runner) { }    
    public async Task<IEnumerable<{{source.Name}}>> All() => await Runner.QueryAsync();
}
""");
        
        spc.AddSource($"{source.Namespace}.{source.Name}.g.cs", code.ToString());
    }
}
