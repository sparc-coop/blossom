using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Sparc.Blossom.Api;
using Sparc.Blossom.Authentication;
using Sparc.Blossom.Data;
using System.Linq.Dynamic.Core;

namespace Sparc.Blossom;

public class BlossomContext(BlossomContextOptions options) : DbContext(options.DbContextOptions), IBlossomEndpointMapper
{
    protected BlossomContextOptions Options { get; } = options;
    public string UserId => Options.HttpContextAccessor?.HttpContext?.User?.Identity?.IsAuthenticated == true ? Options.HttpContextAccessor.HttpContext.User.Id() : "anonymous";

    protected override void ConfigureConventions(ModelConfigurationBuilder configurationBuilder)
    {
        configurationBuilder.Conventions.Add(_ => new BlossomPropertyDiscoveryConvention());
    }

    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var result = await base.SaveChangesAsync(cancellationToken);

        await NotifyAsync();

        return result;
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

    public Task<Dictionary<string, BlossomAggregateMetadata>> Metadata()
    {
        var metadata = new Dictionary<string, BlossomAggregateMetadata>();
        foreach (var entity in Model.GetEntityTypes().Where(x => x.ClrType.IsAssignableTo(typeof(BlossomEntity))))
        {
            var aggregate = new BlossomAggregateMetadata(entity.ClrType);
            var set = GetType().GetMethods().First(x => x.Name == "Set" && x.GetParameters().Count() == 0)!.MakeGenericMethod(entity.ClrType).Invoke(this, null) as IQueryable<BlossomEntity>;

            if (set != null)
            {
                foreach (var property in aggregate.Properties.Where(x => x.CanEdit && x.IsPrimitive))
                {
                    var query = set.GroupBy(property.Name).Select("new { Key, Count() as Count }");
                    property.SetAvailableValues(query.ToDynamicList().ToDictionary(x => (object)x.Key ?? "<null>", x => (int)x.Count));
                }

                foreach (var relationship in aggregate.Properties.Where(x => x.CanEdit && x.IsEnumerable))
                {
                    var query = set.SelectMany(relationship.Name).GroupBy("Id").Select("new { Key, Count() as Count, First() as First }").ToDynamicList();
                    relationship.SetAvailableValues(query.Sum(x => (int)x.Count), query.ToDictionary(x => $"{x.Key}", x => (string)x.First.ToString()));
                }

                metadata.Add(aggregate.Name, aggregate);
            }
        }

        return Task.FromResult(metadata);
    }

    public void MapEndpoints(IEndpointRouteBuilder endpoints)
    {
        endpoints.MapGet("/_metadata", (DbContext context) => (context as BlossomContext)?.Metadata()).CacheOutput();
    }
}