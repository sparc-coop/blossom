using Microsoft.AspNetCore.Components.Web;

namespace Sparc.Platforms.Maui;

public class ConsoleErrorBoundaryLogger : IErrorBoundaryLogger
{
    public ValueTask LogErrorAsync(Exception exception)
    {
        Console.WriteLine($"zxcvb {exception} {exception.StackTrace}");
        return ValueTask.CompletedTask;
    }
}
