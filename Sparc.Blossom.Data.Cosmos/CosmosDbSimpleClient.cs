using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data;

public class CosmosDbSimpleClient<T>(DbContext context, CosmosClient client)
{
    public CosmosClient Client { get; private set; } = client;
    public Container Container { get; } = client.GetContainer(context.Database.GetCosmosDatabaseId(), 
        context.Model.FindEntityType(typeof(T))?.GetContainer() 
        ?? (typeof(T).BaseType?.IsAssignableTo(typeof(BlossomEntity)) == true ? context.Model.FindEntityType(typeof(T).BaseType!)?.GetContainer() : null)
        ?? throw new Exception($"Container name not found for entity type {typeof(T)}"));
    public DbContext Context { get; } = context;
    IReadOnlyList<IProperty>? PartitionKeyProperties { get; } = context.Model.FindEntityType(typeof(T))?.GetPartitionKeyProperties();

    internal bool IsPolymorphicType = typeof(T).BaseType?.IsAssignableTo(typeof(BlossomEntity)) == true
        && context.Model.FindEntityType(typeof(T).BaseType!) != null;

    public static CosmosClient CreateClient(IConfiguration config)
    {
        var options = new CosmosClientOptions
        {
            UseSystemTextJsonSerializerWithOptions = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            },
            ConnectionMode = ConnectionMode.Direct
        };

        var connectionString = config.GetConnectionString("Cosmos")
            ?? config.GetConnectionString("Database")
            ?? throw new Exception("Cosmos connection string not found in configuration.");

        return new CosmosClient(connectionString, options);
    }

    public PartitionKey GetPartitionKey(T item)
    {
        if (item == null || PartitionKeyProperties == null || PartitionKeyProperties.Count == 0)
            return PartitionKey.None;

        var partitionKey = new PartitionKeyBuilder();
        foreach (var property in PartitionKeyProperties)
        {
            var value = item.GetType().GetProperty(property.Name)?.GetValue(item)?.ToString();
            partitionKey.Add(value);
        }

        return partitionKey.Build();
    }
}
