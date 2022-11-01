using Sparc.Kernel;

namespace Sparc.Authentication;

public class SparcUser : Root<string>
{
    public string? SecurityStamp
    {
        get; set;
    }

    public string? UserName { get; set; }
}
