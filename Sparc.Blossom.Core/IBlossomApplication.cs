namespace Sparc.Blossom;

public interface IBlossomApplication
{
    IServiceProvider Services { get; }
    bool IsDevelopment { get; }

    Task RunAsync<TApp>();
    Task RunAsync();
}