using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using Sparc.Blossom.Realtime;

namespace Sparc.Blossom;

public class BlossomContext(BlossomContextOptions options) : DbContext(options.DbContextOptions)
{
    protected BlossomContextOptions Options { get; } = options;
    public string UserId => Options.HttpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true ? Options.HttpContextAccessor.HttpContext.User.Id() : "anonymous";

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new BlossomPropertyDiscoveryConvention());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var modifiedEntities = ChangeTracker.Entries<BlossomEntity>().Where(x => x.State == EntityState.Modified).ToList();
        var changes = GetModifiedPropertiesByEntity(modifiedEntities);

        var result = await base.SaveChangesAsync(cancellationToken);

        await NotifyModifiedPropertiesAsync(changes);
        //await NotifyAsync();

        return result;
    }

    private async Task NotifyModifiedPropertiesAsync(Dictionary<BlossomEntity, List<KeyValuePair<string, object?>>> changes)
    {
        var tasks = changes.Select(async (entityChange) =>
        {
            var entity = entityChange.Key;
            var modifiedFields = entityChange.Value;

            var blossomEvent = new BlossomEvent(entity, modifiedFields);
            await Options.Publisher.Publish(blossomEvent);
        });

        await Task.WhenAll(tasks);
    }

    private static Dictionary<BlossomEntity, List<KeyValuePair<string, object?>>> GetModifiedPropertiesByEntity(List<EntityEntry<BlossomEntity>> modifiedEntities)
    {
        var entityChanges = new Dictionary<BlossomEntity, List<KeyValuePair<string, object?>>>();

        foreach (var entry in modifiedEntities)
        {
            var modifiedFields = new List<KeyValuePair<string, object?>>();

            foreach (var property in entry.Properties)
            {
                if (property.IsModified)
                {
                    modifiedFields.Add(new KeyValuePair<string, object?>(property.Metadata.Name, property.CurrentValue));
                }
            }

            if (modifiedFields.Any())
            {
                entityChanges.Add(entry.Entity, modifiedFields);
            }
        }

        return entityChanges;
    }

    async Task NotifyAsync()
    {
        var domainEvents = ChangeTracker.Entries<BlossomEntity>().SelectMany(x => x.Entity.Publish());

        var tasks = domainEvents
            .Select(async (domainEvent) =>
            {
                await Options.Publisher.Publish(domainEvent);
            });

        await Task.WhenAll(tasks);
    }
}
