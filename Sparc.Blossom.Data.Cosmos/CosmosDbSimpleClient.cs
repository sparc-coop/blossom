using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.Extensions.Configuration;
using System.Text.Json.Serialization;

namespace Sparc.Blossom.Data;

public class CosmosDbSimpleClient<T>(DbContext context, CosmosClient client)
{
    public CosmosClient Client { get; private set; } = client;
    public IEntityType? EntityType { get; }
    public Container Container { get; } = client.GetContainer(context.Database.GetCosmosDatabaseId(), context.Model.FindEntityType(typeof(T))?.GetContainer() ?? throw new Exception($"Container name not found for entity type {typeof(T)}"));
    public DbContext Context { get; } = context;

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
            ?? throw new Exception("Cosmos connection string not found in configuration.");

        return new CosmosClient(connectionString, options);
    }
}
