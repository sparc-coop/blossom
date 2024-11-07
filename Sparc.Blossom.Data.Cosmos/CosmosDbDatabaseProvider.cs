using Microsoft.Azure.Cosmos;
using Microsoft.EntityFrameworkCore;

namespace Sparc.Blossom.Data;

public class CosmosDbDatabaseProvider
{
    public CosmosDbDatabaseProvider(DbContext context, string databaseName)
    {
        Context = context;
        DatabaseName = databaseName;
        Database = context.Database.GetCosmosClient().GetDatabase(DatabaseName);
    }

    public DbContext Context { get; }
    public string DatabaseName { get; }
    public Database Database { get; }
}
