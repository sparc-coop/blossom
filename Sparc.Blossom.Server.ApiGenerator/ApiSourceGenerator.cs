using Microsoft.CodeAnalysis;

namespace Sparc.Blossom.Server.ApiGenerator;

[Generator]
public class ApiSourceGenerator : ISourceGenerator
{
    public void Execute(GeneratorExecutionContext context)
    {
        if (!(context.SyntaxContextReceiver is FeatureSyntaxReceiver receiver))
            return;
        
        var featureAttribute = context.Compilation.GetTypeByMetadataName("Sparc.Blossom.FeatureAttribute");
    }

    public void Initialize(GeneratorInitializationContext context)
    {
        context.RegisterForSyntaxNotifications(() => new FeatureSyntaxReceiver());
    }
}
