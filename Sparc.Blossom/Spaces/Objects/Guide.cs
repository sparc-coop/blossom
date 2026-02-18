using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Spaces;

public class Guide : Post
{
    public Guide() : base()
    {
    }
    public Guide(BlossomSpace space, string text)
        : base(space, BlossomUser.System.Avatar, text)
    {
    }
}
