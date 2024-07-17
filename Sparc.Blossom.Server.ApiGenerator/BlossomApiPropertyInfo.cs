using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.Server.ApiGenerator;

internal class BlossomApiPropertyInfo
{
    internal BlossomApiPropertyInfo(PropertyDeclarationSyntax x)
    {
        Name = x.Identifier.Text;
        Type = x.Type.ToString();
    }

    internal string Name { get; set; }
    internal string Type { get; set; }
}
