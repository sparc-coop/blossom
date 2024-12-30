
namespace Sparc.Blossom;

public class BlossomTimedBackgroundService<T>(IServiceScopeFactory scopes, TimeSpan timespan) 
    : BackgroundService
    where T : IBlossomService
{
    private int executionCount = 0;

    public IServiceScopeFactory Scopes { get; } = scopes;
    public TimeSpan Timespan { get; } = timespan;

    protected override async Task ExecuteAsync(CancellationToken cancellationToken)
    {
        await ExecuteInScopeAsync();

        using PeriodicTimer timer = new(Timespan);

        try
        {
            while (await timer.WaitForNextTickAsync(cancellationToken))
            {
                executionCount++;
                await ExecuteInScopeAsync();
            }
        }
        catch (OperationCanceledException)
        {
        }
    }

    private async Task ExecuteInScopeAsync()
    {
        using var scope = Scopes.CreateScope();
        var service = scope.ServiceProvider.GetRequiredService<T>();
        await service.ExecuteAsync();
    }
}

