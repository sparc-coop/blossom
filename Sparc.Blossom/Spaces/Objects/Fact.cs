using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Spaces;

public class Fact : Post
{
    public Fact() : base()
    {
    }
    public Fact(BlossomSpace space, string text)
        : base(space.SpaceId, BlossomUser.System.Avatar, text)
    {
    }
}

public class Question : Post
{
    public Question() : base()
    {
    }
    public Question(BlossomSpace space, string text)
        : base(space, BlossomUser.System.Avatar, text)
    {
    }
}
