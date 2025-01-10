using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.ApiGenerator;

public class BlossomApiPropertyInfo
{
    public BlossomApiPropertyInfo(PropertyDeclarationSyntax x)
    {
        Name = x.Identifier.Text;
        Type = x.Type.ToString();

        var set = x.AccessorList?.Accessors.FirstOrDefault(y => y.Keyword.Text == "set");
        if (set != null && set.Modifiers.Any())
        {
            SetModifiers = string.Join(" ", set.Modifiers.Select(y => y.Text));
        }
        else if (set == null)
            SetModifiers = "private";
    }

    public BlossomApiPropertyInfo(ParameterSyntax x)
    {
        Name = x.Identifier.Text;
        Type = x.Type!.ToString();
    }

    public string Name { get; set; }
    public string FieldName => $"_{char.ToLower(Name[0])}{Name.Substring(1)}";
    public string Type { get; set; }
    public bool IsNullable => Type.EndsWith("?");
    public string Modifiers => "public";
    public string SetModifiers { get; set; } = "";
    public string PostModifiers(bool isPrivateMember) => IsNullable ? isPrivateMember ? ";" : "" : " = default!;";
}
