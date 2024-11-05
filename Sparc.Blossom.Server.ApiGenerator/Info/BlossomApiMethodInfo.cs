using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.ApiGenerator;

internal class BlossomApiMethodInfo
{
    public BlossomApiMethodInfo(TypeDeclarationSyntax cls, ConstructorDeclarationSyntax constructor)
    {
        Name = cls.Identifier.Text;
        Arguments = string.Join(", ", constructor.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
        Parameters = string.Join(", ", constructor.ParameterList.Parameters.Select(p => p.Identifier));
    }

    internal BlossomApiMethodInfo(MethodDeclarationSyntax method)
    {
        Name = method.Identifier.Text;
        Arguments = string.Join(", ", method.ParameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
        Parameters = string.Join(", ", method.ParameterList.Parameters.Select(p => p.Identifier));
    }

    internal string Name { get; set; }
    internal string Arguments { get; set; }
    internal string Parameters { get; set; }
}
