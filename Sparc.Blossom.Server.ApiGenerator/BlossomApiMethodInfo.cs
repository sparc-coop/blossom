using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.Server.ApiGenerator;

internal class BlossomApiMethodInfo
{
    internal BlossomApiMethodInfo(MethodDeclarationSyntax method)
    {
        ReturnType = method.ReturnType.ToString();
        Name = method.Identifier.Text;
        Parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
    }
    
    internal string ReturnType { get; set; }
    internal string Name { get; set; }
    internal string Parameters { get; set; }
}
