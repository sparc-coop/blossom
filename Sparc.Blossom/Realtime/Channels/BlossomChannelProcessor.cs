using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sparc.Blossom.Realtime;

public class BlossomChannelProcessor(BlossomEvents events, IServiceScopeFactory scopes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (BlossomEvent ev in events.Events.Reader.ReadAllAsync(stoppingToken))
            try
            {
                _ = Process(ev, stoppingToken).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.Error.WriteLine($"Error processing event {ev.Id}: {ex}");
            }
    }

    public async Task Process(BlossomEvent ev, CancellationToken cancellationToken = default)
    {
        using var scope = scopes.CreateScope();
        var handlerType = typeof(BlossomOn<>).MakeGenericType(ev.GetType());
        var handlers = scope.ServiceProvider.GetServices(handlerType);

        await Parallel.ForEachAsync(handlers, cancellationToken, async (handler, ct) =>
        {
            if (ct.IsCancellationRequested)
                return;

            var method = handlerType.GetMethod("ExecuteAsync");
            if (method!.Invoke(handler, [ev]) is Task task)
                await task;
        });
    }
}
