using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace Sparc.Blossom.Realtime;

public record BlossomJob(string Id, Delegate Action, BlossomChannel<BlossomEvent> Channel);
public class BlossomJobProcessor(BlossomEvents events, IServiceScopeFactory scopes) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        await foreach (BlossomJob job in events.Jobs.Reader.ReadAllAsync(stoppingToken))
            try
            {
                _ = Run(job).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                // Log the exception or handle it as needed
                Console.Error.WriteLine($"Error processing job {job.Id}: {ex}");
            }
    }

    public async Task Run(BlossomJob job)
    {
        using var scope = scopes.CreateScope();
        var parameters = job.Action.Method.GetParameters();
        var args = new object?[parameters.Length];

        for (var i = 0; i <  parameters.Length; i++)
        {
            var paramType = parameters[i].ParameterType;
            var service = scope.ServiceProvider.GetService(paramType) 
                ?? throw new InvalidOperationException($"No service registered for type {paramType.FullName}");
            args[i] = service;
        }

        var result = job.Action.DynamicInvoke(args);
        if (result is Task taskResult)
            await taskResult;

        BlossomEvents.Complete(job);
    }
}
