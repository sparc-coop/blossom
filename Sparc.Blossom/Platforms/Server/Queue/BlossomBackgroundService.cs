namespace Sparc.Blossom;

public class BlossomBackgroundService<T> : BackgroundService where T : class
{
    public BlossomBackgroundService(ILogger<BlossomBackgroundService<T>> logger, IServiceScopeFactory scopes, IConfiguration config, BlossomQueue<T> queue)
    {
        _logger = logger;
        Scopes = scopes;
        Config = config;
        Queue = queue;
        _executors = new Task[_executorsCount];
    }

    private readonly ILogger<BlossomBackgroundService<T>> _logger;
    public IServiceScopeFactory Scopes { get; }
    public IConfiguration Config { get; }
    public BlossomQueue<T> Queue { get; }
    private readonly Task[] _executors;
    private readonly int _executorsCount = 8;

    protected override Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation($"Blossom Runner is running.");

        for (var i = 0; i < _executorsCount; i++)
        {
            var processor = new Task(async () => await ProcessAsync(stoppingToken));
            _executors[i] = processor;
            processor.Start();
        }

        return Task.CompletedTask;
    }

    private async Task ProcessAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            var workItem = await Queue.GetAsync(stoppingToken);
            _logger.LogInformation("Starting execution of {WorkItem}.", nameof(workItem));

            try
            {
                using var scope = Scopes.CreateScope();
                var item = scope.ServiceProvider.GetRequiredService<T>();
                
                await workItem(item, stoppingToken);
                _logger.LogInformation("Execution of {WorkItem} complete.", nameof(workItem));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex,
                    "Error occurred executing {WorkItem}.", nameof(workItem));
            }
        }
    }

    public override Task StopAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Blossom Runner is stopping.");

        if (_executors != null)
        {
            // wait for _executors completion
            Task.WaitAll(_executors, stoppingToken);
        }

        return Task.CompletedTask;
    }
}
