using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace Sparc.Blossom.ApiGenerator;

[Generator]
internal class BlossomApiGenerator() : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entities = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => Where(s, "BlossomEntity", "BlossomAggregate", "BlossomUser"),
                transform: static (ctx, _) => new BlossomApiInfo((TypeDeclarationSyntax)ctx.Node)
            ).Where(static m => m is not null)
            .Collect();

        context.RegisterSourceOutput(entities, Generate);
    }

    bool Where(SyntaxNode syntax, params string[] baseTypes)
    {
        return
            syntax is TypeDeclarationSyntax type
            && type.BaseList != null
            && type.BaseList.Types.Any(t => baseTypes.Any(y => t.Type.ToString().Contains(y)));
    }

    private void Generate(SourceProductionContext context, ImmutableArray<BlossomApiInfo> sources)
    {
        var baseTypes = sources.GroupBy(x => x.EntityName);

        context.AddSource($"BlossomApi.g.cs", Surface(baseTypes));
        foreach (var baseType in baseTypes)
        {
            var name = baseType.OrderBy(x => x.IsAggregate ? 0 : 1).First().PluralName;
            context.AddSource($"{name}.g.cs", Code(baseType));
        }
    }

    private string Surface(IEnumerable<IGrouping<string?, BlossomApiInfo>> sources)
    {
        var apis = new StringBuilder();
        List<string> injectors = [];

        foreach (var source in sources)
        {
            var api = source.OrderBy(x => x.IsAggregate ? 0 : 1).First();
            apis.AppendLine($@"public {api.PluralName} {api.PluralName} {{ get; }} = {api.PluralName.ToLower()};");
            injectors.Add($"{api.PluralName} {api.PluralName.ToLower()}");
        }

        var constructor = string.Join(", ", injectors);

        return $$"""
namespace Sparc.Blossom.Api;
public class BlossomApi({{constructor}}) : BlossomApiProxy
{
    {{apis}}
}
""";
    }

    static string Code(IGrouping<string?, BlossomApiInfo> sources)
    {
        var commands = new StringBuilder();
        var constructors = new StringBuilder();
        var queries = new StringBuilder();
        var api = sources.OrderBy(x => x.IsAggregate ? 0 : 1).First();

        foreach (var comment in api.Comments)
            commands.AppendLine("// " + comment);

        foreach (var source in sources.Where(x => x.IsEntity))
        {
            foreach (var constructor in source.Constructors)
            {
                constructors.AppendLine($@"public async Task<{source.Name}> Create({constructor.Arguments}) => await Runner.Create({constructor.Parameters});");
            }
        }

        foreach (var source in sources.Where(x => !x.IsEntity))
        {
            foreach (var query in source.Methods.Where(x => x.IsQuery))
            { 
                var parameterPrefix = query.Arguments.Length > 0 ? ", " : "";
                    queries.AppendLine($@"public async Task<IEnumerable<{source.BaseOfName}>> {query.Name}({query.Arguments}) => await Runner.ExecuteQuery(""{query.Name}""{parameterPrefix}{query.Parameters});");
            }

            foreach (var query in source.Methods.Where(x => !x.IsQuery))
            {
                var parameterPrefix = query.Arguments.Length > 0 ? ", " : "";
                queries.AppendLine($@"public async {query.ReturnType} {query.Name}({query.Arguments}) => await Runner.ExecuteQuery<{query.ReturnTypeWithoutTask}>(""{query.Name}""{parameterPrefix}{query.Parameters});");
            }
        }

        return $$"""
namespace Sparc.Blossom.Api;
#nullable enable
public partial class {{api.PluralName}} : BlossomAggregateProxy<{{api.EntityName}}>
{
    public {{api.PluralName}}(IRunner<{{api.EntityName}}> runner) : base(runner) { }

    {{constructors}}
    {{commands}}
    {{queries}}
}
""";
    }
}
