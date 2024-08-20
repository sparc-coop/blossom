using Microsoft.CodeAnalysis;
using System.Text;

namespace Sparc.Blossom.ApiGenerator;

[Generator]
internal class BlossomApiQueryGenerator() : BlossomGenerator("BlossomQuery", Code)
{
    static string Code(BlossomApiInfo source)
    {
        var queries = new StringBuilder();
        foreach (var constructor in source.Constructors)
        {
            var parameterPrefix = constructor.Arguments.Length > 0 ? ", " : "";
            //var returnType = properties.Length > 0 ? source.Name : source.BaseName;
            queries.AppendLine($@"public async Task<IEnumerable<{source.BaseName}>> {source.Name}({constructor.Arguments}) => await Runner.QueryAsync(""{source.Name}""{parameterPrefix}{constructor.Parameters});");
        }

        return $$"""
namespace Sparc.Blossom.Api;
{{source.Nullable}}
public partial class {{source.BasePluralName}} : BlossomApiContext<{{source.BaseName}}>
{
    {{queries}}
}
""";
    }
}
