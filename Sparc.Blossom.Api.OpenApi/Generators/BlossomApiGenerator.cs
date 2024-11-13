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
        foreach (var baseType in baseTypes)
        {
            context.AddSource($"{baseType.Key}EndpointMapper.g.cs", Code(baseType));
        }
    }

    static string Code(IGrouping<string?, BlossomApiInfo> sources)
    {
        var commands = new StringBuilder();
        var constructors = new StringBuilder();
        var queries = new StringBuilder();
        var usings = new StringBuilder();
        var api = sources.OrderBy(x => x.IsEntity ? 0 : 1).First();

        foreach (var source in sources)
        {
            foreach (var method in source.Methods)
            {
                var parameterPrefix = method.Arguments.Length > 0 ? ", " : "";
                commands.AppendLine($@"group.MapPut(""{{id}}/{method.Name}"", async (Sparc.Blossom.Api.IRunner<{source.Name}> runner, string id{parameterPrefix}{method.Arguments}) => await runner.ExecuteAsync(id, ""{method.Name}""{parameterPrefix}{method.Parameters}));");
            }

            foreach (var constructor in source.Constructors)
            {
                var parameterPrefix = constructor.Arguments.Length > 0 ? ", " : "";
                if (source.IsEntity)
                {
                    constructors.AppendLine($@"group.MapPost("""", async (Sparc.Blossom.Api.IRunner<{source.Name}> runner{parameterPrefix}{constructor.Arguments}) => await runner.CreateAsync({constructor.Parameters}));");

                }
                else
                {
                    //var returnType = properties.Length > 0 ? source.Name : source.BaseName;
                    queries.AppendLine($@"group.MapGet(""{source.Name}"", async (Sparc.Blossom.Api.IRunner<{source.BaseOfName}> runner{parameterPrefix}{constructor.Arguments}) => await runner.QueryAsync(""{source.Name}""{parameterPrefix}{constructor.Parameters}));");
                }
            }
        }

        foreach (var u in sources.SelectMany(x => x.Usings).Distinct())
        {
            usings.AppendLine(u);
        }

        return $$"""
{{usings}}
#nullable enable

namespace {{api.Namespace}}
{
    public partial class {{api.Name}}EndpointMapper : Sparc.Blossom.Api.IBlossomEndpointMapper
    {
        public void MapEndpoints(IEndpointRouteBuilder endpoints)
        {
            var group = endpoints.MapGroup("{{api.PluralName.ToLower()}}");

            group.MapGet("{id}", async (Sparc.Blossom.Api.IRunner<{{api.Name}}> runner, string id) => await runner.GetAsync(id));
            group.MapDelete("{id}", async (Sparc.Blossom.Api.IRunner<{{api.Name}}> runner, string id) => await runner.DeleteAsync(id));
    
            {{constructors}}
            {{commands}}
            {{queries}}
        }
    }
}
""";
    }
}
