using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.ApiGenerator;

internal class BlossomApiPropertyInfo
{
    internal BlossomApiPropertyInfo(PropertyDeclarationSyntax x)
    {
        Name = x.Identifier.Text;
        Type = x.Type.ToString();

        var set = x.AccessorList?.Accessors.FirstOrDefault(y => y.Keyword.Text == "set");
        if (set != null && set.Modifiers.Any())
        {
            SetModifiers = string.Join(" ", set.Modifiers.Select(y => y.Text));
        }
    }

    internal BlossomApiPropertyInfo(ParameterSyntax x)
    {
        Name = x.Identifier.Text;
        Type = x.Type!.ToString();
    }

    internal string Name { get; set; }
    internal string Type { get; set; }
    internal bool IsNullable => Type.EndsWith("?");
    internal string Modifiers => "public";
    internal string SetModifiers { get; set; } = "";
    internal string PostModifiers => IsNullable ? "" : " = default!;";
}
