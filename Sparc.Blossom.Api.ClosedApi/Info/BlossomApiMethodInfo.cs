using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.ApiGenerator;

public class BlossomApiMethodInfo(ParameterListSyntax parameterList)
{
    public BlossomApiMethodInfo(TypeDeclarationSyntax cls, ConstructorDeclarationSyntax constructor) : this(constructor.ParameterList)
    {
        Name = cls.Identifier.Text;
    }

    public BlossomApiMethodInfo(MethodDeclarationSyntax method) : this(method.ParameterList)
    {
        Name = method.Identifier.Text;
        ReturnType = method.ReturnType.ToString();
    }

    public bool IsQuery => ReturnType?.Contains("BlossomQuery") ?? false;
    public string Name { get; set; } = "PrimaryConstructor";
    public string? ReturnType { get; }
    public string? ReturnTypeWithoutTask => ReturnType?.StartsWith("Task") == true
        ? ReturnType.Substring(0, ReturnType.Length - 1).Replace("Task<", "")
        : ReturnType;
    public string Arguments { get; set; } = string.Join(", ", parameterList.Parameters.Select(p => $"{p.Type} {p.Identifier}"));
    public string Parameters { get; set; } = string.Join(", ", parameterList.Parameters.Select(p => p.Identifier));
}
