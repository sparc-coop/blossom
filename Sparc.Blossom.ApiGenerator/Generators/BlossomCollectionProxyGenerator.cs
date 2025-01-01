using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Immutable;
using System.Text;

namespace Sparc.Blossom.ApiGenerator;

[Generator]
internal class BlossomCollectionProxyGenerator() : IIncrementalGenerator
{
    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entities = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => Where(s, "BlossomEntity", "BlossomCollection"),
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
            var name = baseType.OrderBy(x => x.IsCollection ? 0 : 1).First().PluralName;
            context.AddSource($"{name}.g.cs", Code(baseType));
        }
    }

    private string Surface(IEnumerable<IGrouping<string?, BlossomApiInfo>> sources)
    {
        var apis = new StringBuilder();
        List<string> injectors = [];

        foreach (var source in sources)
        {
            var api = source.OrderBy(x => x.IsCollection ? 0 : 1).First();
            apis.AppendLine($@"public {api.PluralProxyName} {api.PluralName} {{ get; }} = {api.PluralName.ToLower()};");
            injectors.Add($"{api.PluralProxyName} {api.PluralName.ToLower()}");
        }

        var constructor = string.Join(", ", injectors);

        return $$"""
namespace Sparc.Blossom.Api;
public class BlossomApi({{constructor}}) : IBlossomApi
{
    {{apis}}
}
""";
    }

    static string Code(IGrouping<string?, BlossomApiInfo> sources)
    {
        var constructors = new StringBuilder();
        var queries = new StringBuilder();
        var api = sources.OrderBy(x => x.IsCollection ? 0 : 1).First();

        foreach (var source in sources.Where(x => !x.IsEntity))
        {    // public async Task<T?> Get(object id) => await Repository.FindAsync(id);

            foreach (var query in source.Methods.Where(x => x.IsQuery))
            {
                queries.AppendLine($@"public async Task<IEnumerable<{api.ProxyName}>> {query.Name}({query.Arguments}) => await GetAllAsync(new {query.Name}({query.Parameters}));");
            }
        }

        return $$"""
using Sparc.Blossom.Api;
namespace {{api.Namespace}};
#nullable enable
public partial class {{api.PluralProxyName}} : BlossomCollectionProxy<{{api.EntityName}}, {{api.ProxyName}}>
{
    public {{api.PluralProxyName}}(IRepository<{{api.EntityName}}> repository) : base(repository) { }

    {{queries}}
}
""";
    }
}
