using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data;

public class CosmosDbSimpleClient<T>
{
    public CosmosClient Client { get; }
    public string DatabaseName { get; }
    public IEntityType? EntityType { get; }
    public Container Container { get; }
    public DbContext Context { get; }

    public CosmosDbSimpleClient(DbContext context, IConfiguration config)
    {
        DatabaseName = context.Database.GetCosmosDatabaseId();
        EntityType = context.Model.FindEntityType(typeof(T));
        
        var efClient = context.Database.GetCosmosClient();
        var containerName = (EntityType?.GetContainer())
            ?? throw new Exception($"Container name not found for entity type {typeof(T)}");

        var options = new CosmosClientOptions
        {
            UseSystemTextJsonSerializerWithOptions = new()
            {
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                PropertyNamingPolicy = new CamelCaseIdNamingPolicy(),
                MaxDepth = 64
            }
        };

        var connectionString = config.GetConnectionString("Cosmos")
            ?? throw new Exception("Cosmos connection string not found in configuration.");

        Client = new CosmosClient(connectionString, options);
        Container = Client.GetContainer(DatabaseName, containerName);
        Context = context;
    }
}
