using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.ApiGenerator;

internal class BlossomApiPropertyInfo
{
    internal BlossomApiPropertyInfo(PropertyDeclarationSyntax x)
    {
        Name = x.Identifier.Text;
        Type = x.Type.ToString();
    }

    internal BlossomApiPropertyInfo(ParameterSyntax x)
    {
        Name = x.Identifier.Text;
        Type = x.Type!.ToString();
    }

    internal string Name { get; set; }
    internal string Type { get; set; }
    internal bool IsNullable => Type.EndsWith("?");
    internal string Modifiers => "public" + (IsNullable ? "" : " required");
}
