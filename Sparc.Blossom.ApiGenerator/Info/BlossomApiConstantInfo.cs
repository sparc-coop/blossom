using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Sparc.Blossom.ApiGenerator;

internal class BlossomApiFieldInfo
{
    internal BlossomApiFieldInfo(MemberDeclarationSyntax x)
    {
        Body = x.ToFullString();
    }

    internal string Body { get; set; }
}
