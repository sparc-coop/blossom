using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.Server.ApiGenerator;

internal class BlossomApiInfo
{
    internal BlossomApiInfo(ClassDeclarationSyntax cls)
    {
        Name = cls.Identifier.Text;
        Namespace = cls.FirstAncestorOrSelf<FileScopedNamespaceDeclarationSyntax>()?.Name.ToString() 
            ?? cls.FirstAncestorOrSelf<NamespaceDeclarationSyntax>()?.Name.ToString()
            ?? "";
        Route = cls.Identifier.Text;
        Methods = cls.Members.OfType<MethodDeclarationSyntax>()
            .Where(x => x.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
            .Select(x => new BlossomApiMethodInfo(x))
            .ToArray();
        Usings = cls.SyntaxTree.GetRoot().DescendantNodes().OfType<UsingDirectiveSyntax>().Select(x => x.ToString()).ToArray();
    }
    
    internal string Name { get; set; }
    internal string Namespace { get; set; }
    internal string Route { get; set; }
    internal string[] Usings { get; set; }
    public BlossomApiMethodInfo[] Methods { get; internal set; }
}

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
