using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Text;

namespace Sparc.Blossom.Server.ApiGenerator;

[Generator]
public class BlossomApiGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var targets = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => Where(s),
                transform: static (ctx, _) => Select(ctx)
            ).Where(static m => m is not null);

        context.RegisterSourceOutput(targets, static (spc, source) => Generate(source, spc));
    }

    static bool Where(SyntaxNode syntax)
    {
        if (syntax is not ClassDeclarationSyntax classDeclaration)
            return false;

        return
            classDeclaration.BaseList != null &&
            (classDeclaration.BaseList.Types.Any(t => t.Type.ToString().Contains("Entity")) == true
            || classDeclaration.BaseList.Types.Any(t => t.Type.ToString().Contains("Aggregate")) == true);
    }

    static BlossomApiInfo Select(GeneratorSyntaxContext ctx) => new((ClassDeclarationSyntax)ctx.Node);

    static void Generate(BlossomApiInfo source, SourceProductionContext spc)
    {
        var usings = string.Join("\n", source.Usings);
        
        var interfaces = new StringBuilder();
        foreach (var method in source.Methods)
        {
            interfaces.AppendLine($@"public {method.ReturnType} {method.Name}({method.Parameters});");
        }

        foreach (var property in source.Properties)
        {
            interfaces.AppendLine($@"public {property.Type} {property.Name} {{ get; set; }}");
        }

        var code = new StringBuilder();
        code.Append($$"""
{{usings}}
namespace {{source.Namespace}}
{
    public interface I{{source.Name}} 
    {
        {{interfaces}}
    }

    public partial class {{source.Name}} : I{{source.Name}}
    {
    }
}
""");
        
        spc.AddSource($"{source.Name}.g.cs", code.ToString());
    }
}
