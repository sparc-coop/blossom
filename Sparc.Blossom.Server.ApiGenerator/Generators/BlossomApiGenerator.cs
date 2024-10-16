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
                predicate: (s, _) => Where(s, "BlossomEntity", "BlossomQuery"),
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
            context.AddSource($"{baseType.Key}.g.cs", Code(baseType));
        }
    }

    private string Surface(IEnumerable<IGrouping<string?, BlossomApiInfo>> sources)
    {
        var apis = new StringBuilder();
        List<string> injectors = [];

        foreach (var source in sources)
        {
            var api = source.OrderBy(x => x.IsEntity ? 0 : 1).First();
            apis.AppendLine($@"public {api.PluralName} {api.PluralName} {{ get; }} = {api.PluralName.ToLower()};");
            injectors.Add($"{api.PluralName} {api.PluralName.ToLower()}");
        }

        var constructor = string.Join(", ", injectors);

        return $$"""
using Sparc.Blossom.Data;
namespace Sparc.Blossom.Api;
public class BlossomApi({{constructor}}) : IBlossomApi
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
        var api = sources.OrderBy(x => x.IsEntity ? 0 : 1).First();

        foreach (var source in sources)
        {
            //foreach (var method in source.Methods)
            //{
            //    var parameterPrefix = method.Arguments.Length > 0 ? ", " : "";
            //    commands.AppendLine($@"public async Task {method.Name}({method.Arguments}) => await Runner.ExecuteAsync(Id, ""{method.Name}""{parameterPrefix}{method.Parameters});");
            //}

            foreach (var constructor in source.Constructors)
            {
                if (source.IsEntity)
                {
                    constructors.AppendLine($@"public async Task<{source.Name}> Create({constructor.Arguments}) => await Runner.CreateAsync({constructor.Parameters});");
                }
                else
                {
                    var parameterPrefix = constructor.Arguments.Length > 0 ? ", " : "";
                    //var returnType = properties.Length > 0 ? source.Name : source.BaseName;
                    queries.AppendLine($@"public async Task<IEnumerable<{source.BaseOfName}>> {source.Name}({constructor.Arguments}) => await Runner.QueryAsync(""{source.Name}""{parameterPrefix}{constructor.Parameters});");
                }
            }
        }

        return $$"""
namespace Sparc.Blossom.Api;
#nullable enable
public partial class {{api.PluralName}} : BlossomApiContext<{{api.EntityName}}>
{
    public {{api.PluralName}}(IRunner<{{api.EntityName}}> runner) : base(runner) { }

    {{constructors}}
    {{commands}}
    {{queries}}
}
""";
    }
}
