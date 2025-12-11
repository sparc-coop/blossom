using Sparc.Blossom.Authentication;
using Sparc.Blossom.Content;

namespace Sparc.Blossom;

public class BlossomPost : TextContent
{
    public BlossomPost() : base()
    { }

    public BlossomPost(string domain, string spaceId, Language language, string text, BlossomUser user)
        : base(domain, spaceId, language, text, user)
    { }

    public string PostId { get { return Id; } set { Id = value; } }
}
