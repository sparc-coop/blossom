using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace Sparc.Blossom.Server.ApiGenerator;

[Generator]
public class BlossomApiGenerator : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entities = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: static (s, _) => Where(s, "BlossomEntity"),
                transform: static (ctx, _) => Select(ctx)
            ).Where(static m => m is not null);

        context.RegisterSourceOutput(entities, static (spc, source) => Generate(source, spc));
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
        
        var properties = new StringBuilder();
        foreach (var property in source.Properties)
            properties.AppendLine($@"public {property.Type} {property.Name} {{ get; set; }}");

        var commands = new StringBuilder();
        foreach (var method in source.Methods)
        {
            var parameterPrefix = method.Arguments.Length > 0 ? ", " : "";
            commands.AppendLine($@"public async Task {method.Name}({method.Arguments}) => await Runner.ExecuteAsync(Id, ""{method.Name}""{parameterPrefix}{method.Parameters});");
        }

        var constructors = new StringBuilder();
        foreach (var constructor in source.Constructors)
        {
            constructors.AppendLine($@"public async Task<{source.Name}> Create({constructor.Arguments}) => await Runner.CreateAsync({constructor.Parameters});");
        }

        var code = new StringBuilder();
        code.Append($$"""
{{usings}}
namespace {{source.Namespace}}.Client
{
    public partial class {{source.PluralName}} : BlossomApiContext<{{source.Name}}>
    {
        public {{source.PluralName}}(IRunner<{{source.Name}}> runner) : base(runner) { }

        {{constructors}}
        public async Task Delete(object id) => await Runner.DeleteAsync(id);
    }

    public class {{source.Name}} : BlossomEntityProxy<{{source.Name}}, {{source.BaseName}}>
    {
        {{properties}}
        {{commands}}
    }
}    
""");
        
        spc.AddSource($"{source.Name}.g.cs", code.ToString());
    }
}
