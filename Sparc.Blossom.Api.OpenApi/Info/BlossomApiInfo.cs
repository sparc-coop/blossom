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

        if (type.TypeParameterList?.Parameters.Any() == true)
            OfName = $"<{type.TypeParameterList.Parameters.FirstOrDefault()}>";

        if (type.BaseList != null)
        {
            var baseType = type.BaseList.Types.FirstOrDefault()?.ToString();
            if (baseType != null)
            {
                var genericArguments = baseType.Split('<', ',', '>');

                BaseName = genericArguments[0];
                BaseOfName = genericArguments.Length > 1 ? genericArguments[1] : null;
                BasePluralName = (BaseOfName ?? BaseName) + "s";
            }
        }

        PluralName = EntityName + "s";

        Methods = type.Public<MethodDeclarationSyntax>()
            .Where(x => x.Identifier.Text != "ToString")
            .Select(x => new BlossomApiMethodInfo(x))
            .ToList();

        Properties = type.Public<PropertyDeclarationSyntax>()
            .Select(x => new BlossomApiPropertyInfo(x))
            .ToList();

        Constructors = type.Public<ConstructorDeclarationSyntax>()
            .Select(x => new BlossomApiMethodInfo(type, x))
            .ToList();

        Constants = type.Public<FieldDeclarationSyntax>()
            .Where(x => x.Modifiers.Any(m => m.IsKind(SyntaxKind.StaticKeyword)))
            .Select(x => new BlossomApiFieldInfo(x))
            .ToList();

        if (type is RecordDeclarationSyntax rec && !Properties.Any())
            Properties = rec.ParameterList?.Parameters.Select(x => new BlossomApiPropertyInfo(x)).ToList() ?? [];

        Nullable = Properties.Any(x => x.IsNullable) ? "#nullable enable" : "#nullable disable";
    }

    internal string Name { get; }
    public string PluralName { get; }
    internal string? OfName { get; set; }
    internal string? BaseOfName { get; set; }
    internal string? BaseName { get; set; }
    internal string? BasePluralName { get; set; }
    internal string Namespace { get; }
    internal string[] Usings { get; }
    internal string Nullable { get; } = "";
    public List<BlossomApiMethodInfo> Methods { get; }
    public List<BlossomApiMethodInfo> Constructors { get; }
    public List<BlossomApiPropertyInfo> Properties { get; }
    public List<BlossomApiFieldInfo> Constants { get; }
    public bool IsEntity => BaseName?.Contains("BlossomEntity") == true;
    public string EntityName => IsEntity ? Name : (BaseOfName ?? Name);
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
