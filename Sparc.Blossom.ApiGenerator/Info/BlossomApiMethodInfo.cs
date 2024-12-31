using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.ApiGenerator;

internal class BlossomApiMethodInfo(ParameterListSyntax parameterList)
{
    public BlossomApiMethodInfo(TypeDeclarationSyntax cls, ConstructorDeclarationSyntax constructor) : this(constructor.ParameterList)
    {
        Name = cls.Identifier.Text;
    }

    internal BlossomApiMethodInfo(MethodDeclarationSyntax method) : this(method.ParameterList)
    {
        Name = method.Identifier.Text;
        ReturnType = method.ReturnType.ToString();
    }

    public bool IsQuery => ReturnType?.Contains("BlossomQuery") ?? false;
    internal string Name { get; set; } = "PrimaryConstructor";
    public string? ReturnType { get; }
    internal string Arguments { get; set; } = string.Join(", ", parameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
    internal string Parameters { get; set; } = string.Join(", ", parameterList.Parameters.Select(p => p.Identifier));
}
