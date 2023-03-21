using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Collections.Generic;

namespace Sparc.Blossom.Server.ApiGenerator;

internal class FeatureSyntaxReceiver : ISyntaxContextReceiver
{
    public INamedTypeSymbol FeatureSymbol;

    public List<FeatureInfo> Features = new();

    public void OnVisitSyntaxNode(GeneratorSyntaxContext context)
    {
        if (context.Node is ClassDeclarationSyntax)
        {
            if (context.SemanticModel.GetDeclaredSymbol(context.Node) is INamedTypeSymbol classSymbol 
                && InheritsFrom(classSymbol, FeatureSymbol))
            {
                Features.Add(new() { Name = classSymbol.Name, Route = $"api/{classSymbol.Name}" });
            }
        }
    }

    private static bool InheritsFrom(INamedTypeSymbol classDeclaration, INamedTypeSymbol targetBaseType)
    {
        var currentDeclared = classDeclaration;
        while (currentDeclared.BaseType != null)
        {
            var currentBaseType = currentDeclared.BaseType;
            if (currentBaseType.Equals(targetBaseType, SymbolEqualityComparer.Default))
            {
                return true;
            }

            currentDeclared = currentDeclared.BaseType;
        }

        return false;
    }
}

