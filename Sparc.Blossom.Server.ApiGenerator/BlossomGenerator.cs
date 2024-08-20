using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.ApiGenerator;

internal abstract class BlossomGenerator(string type, Func<BlossomApiInfo, string> code) : IIncrementalGenerator
{
    internal string Type = type;
    internal Func<BlossomApiInfo, string> CodeGenerator { get; } = code;

    public void Initialize(IncrementalGeneratorInitializationContext context)
    {
        var entities = context.SyntaxProvider
            .CreateSyntaxProvider(
                predicate: (s, _) => Where(s, Type),
                transform: static (ctx, _) => Select(ctx)
            ).Where(static m => m is not null);

        context.RegisterSourceOutput(entities, Generate);
    }

    protected virtual bool Where(SyntaxNode syntax, string baseType)
    {
        return
            syntax is TypeDeclarationSyntax type
            && type.BaseList != null
            && type.BaseList.Types.Any(t => t.Type.ToString().Contains(baseType));
    }

    internal static BlossomApiInfo Select(GeneratorSyntaxContext ctx)
        => new((TypeDeclarationSyntax)ctx.Node);

    void Generate(SourceProductionContext spc, BlossomApiInfo source)
    {
        spc.AddSource($"{source.Name}.g.cs", CodeGenerator(source));
    }
}

internal abstract class BlossomGenerator<T>(Func<BlossomApiInfo, string> code) 
    : BlossomGenerator(typeof(T).Name, code) 
    where T : SyntaxNode
{

    protected override bool Where(SyntaxNode syntax, string baseType)
    {
            return syntax is T
                && syntax is TypeDeclarationSyntax type
                && type.IsPublic()
                && (type is RecordDeclarationSyntax || type.Members.Any(x => x is PropertyDeclarationSyntax p && p.IsPublic()));

    }
}
