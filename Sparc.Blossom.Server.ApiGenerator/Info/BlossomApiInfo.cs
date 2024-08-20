using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.ApiGenerator;

internal class BlossomApiInfo
{
    internal BlossomApiInfo(TypeDeclarationSyntax type)
    {
        Usings = type.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Select(x => x.ToString()).ToArray();
        if (!Usings.Contains("using Sparc.Blossom;"))
            Usings = Usings.Append("using Sparc.Blossom;").ToArray();

        Namespace = type.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()?.Name.ToString()
            ?? type.FirstAncestorOrSelf<NamespaceDeclarationSyntax>()?.Name.ToString()
            ?? "";


        Name = type.Identifier.Text;
        PluralName = Name + "Api";

        if (type.TypeParameterList?.Parameters.Any() == true)
            OfName = $"<{type.TypeParameterList.Parameters.FirstOrDefault()}>";

        if (type.BaseList != null)
        {
            var baseType = type.BaseList.Types.FirstOrDefault()?.ToString();
            if (baseType != null && baseType.Contains("BlossomRecord"))
            {
                BaseName = "BlossomRecord";
            }
            else if (baseType != null && baseType.Contains("<"))
            {
                // get the first generic argument of the base class
                var genericArgument = baseType.Split('<', ',', '>')[1];

                BaseName = genericArgument;
                BasePluralName = genericArgument + "Api";
            }
        }

        Methods = type.Public<MethodDeclarationSyntax>()
            .Select(x => new BlossomApiMethodInfo(x))
            .ToList();

        Properties = type.Public<PropertyDeclarationSyntax>()
            .Select(x => new BlossomApiPropertyInfo(x))
            .ToList();

        Constructors = type.Public<ConstructorDeclarationSyntax>()
            .Select(x => new BlossomApiMethodInfo(type, x))
            .ToList();

        if (type is RecordDeclarationSyntax rec && !Properties.Any())
            Properties = rec.ParameterList?.Parameters.Select(x => new BlossomApiPropertyInfo(x)).ToList() ?? [];

        Nullable = Properties.Any(x => x.IsNullable) ? "#nullable enable" : "#nullable disable";
    }

    internal string Name { get; }
    public string PluralName { get; }
    internal string? OfName { get; set; }
    internal string? BaseName { get; set; }
    internal string? BasePluralName { get; set; }
    internal string Namespace { get; }
    internal string[] Usings { get; }
    internal string Nullable { get; } = "";
    public List<BlossomApiMethodInfo> Methods { get; }
    public List<BlossomApiMethodInfo> Constructors { get; }
    public List<BlossomApiPropertyInfo> Properties { get; }
}

public static class BlossomApiInfoExtensions
{ 
    public static IEnumerable<T> Public<T>(this TypeDeclarationSyntax cls) where T : MemberDeclarationSyntax
    {
        return cls.Members.OfType<T>().Where(x => x.IsPublic());
    }

    public static bool IsPublic(this MemberDeclarationSyntax member)
    {
        return member.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword));
    }
}
