using Sparc.Blossom;
using Sparc.Blossom.Data;

namespace Blossom;
public static class Program
{
    public static void Main(string[] args)
    {
        var builder = BlossomApplication.CreateBuilder(args);
        builder.Build().Run();
    }
}