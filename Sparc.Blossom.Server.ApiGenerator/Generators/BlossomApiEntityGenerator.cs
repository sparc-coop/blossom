using Microsoft.CodeAnalysis;
using System.Text;

namespace Sparc.Blossom.ApiGenerator;

[Generator]
internal class BlossomApiEntityGenerator() : BlossomGenerator("BlossomEntity", Code)
{
    static string Code(BlossomApiInfo source)
    {
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

        return $$"""
namespace Sparc.Blossom.Api;
#nullable enable
public partial class {{source.PluralName}} : BlossomApiContext<{{source.Name}}>
{
    public {{source.PluralName}}(IRunner<{{source.Name}}> runner) : base(runner) { }

    {{constructors}}
    public async Task Delete(object id) => await Runner.DeleteAsync(id);
    public async Task<{{source.Name}}?> Get(object id) => await Runner.GetAsync(id);

    {{commands}}
}

public partial class {{source.Name}} : BlossomEntityProxy<{{source.Name}}, {{source.BaseName}}>
{
    {{commands}}
}
""";
    }
}
