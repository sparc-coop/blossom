using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Spaces;

public class Fact : Post
{
    public Fact() : base()
    {
    }
    public Fact(BlossomSpace space, string text)
        : base(space, BlossomUser.System.Avatar, text)
    {
    }

    public override float Mass => 1;
}

public class Question : Post
{
    public bool IsActive { get; set; }
    
    public Question() : base()
    {
    }

    public Question(BlossomSpace space, string text)
        : base(space, BlossomUser.System.Avatar, text)
    {
    }

    public override float Mass => IsActive ? 10 : 0;
}
