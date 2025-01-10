using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.ApiGenerator;

public class BlossomApiFieldInfo
{
    public BlossomApiFieldInfo(MemberDeclarationSyntax x)
    {
        Body = x.ToFullString();
    }

    public string Body { get; set; }
}
