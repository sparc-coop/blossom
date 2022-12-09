using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public class CosmosDbDatabaseProvider
{
    public CosmosDbDatabaseProvider(DbContext context, string databaseName)
    {
        DatabaseName = databaseName;
        Database = context.Database.GetCosmosClient().GetDatabase(DatabaseName);
    }

    public string DatabaseName { get; }
    public Microsoft.Azure.Cosmos.Database Database { get; }
}
