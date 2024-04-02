using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.Server.ApiGenerator;

internal class BlossomApiMethodInfo
{
    public BlossomApiMethodInfo(ClassDeclarationSyntax cls, ConstructorDeclarationSyntax constructor)
    {
        Name = cls.Identifier.Text;
        Parameters = string.Join(", ", constructor.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
    }

    internal BlossomApiMethodInfo(MethodDeclarationSyntax method)
    {
        Name = method.Identifier.Text;
        Parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
    }
    
    internal string Name { get; set; }
    internal string Parameters { get; set; }
}
