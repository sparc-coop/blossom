# Sparc.Database.Cosmos

[![Nuget](https://img.shields.io/nuget/v/Sparc.Database.Cosmos?label=Sparc.Database.Cosmos)](https://www.nuget.org/packages/Sparc.Database.Cosmos/)

The `Sparc.Database.Cosmos` plugin is primarily an implementation of `IRepository<T>` that uses [Azure Cosmos DB](https://docs.microsoft.com/en-us/azure/cosmos-db/introduction) via Entity Framework for all of its persistence operations.

Add this plugin to your Features Project if you'd like to use Azure Cosmos DB as your app's database provider.

## Get Started with Sparc.Database.Cosmos

In Your Features Project:

1. Add the `Sparc.Database.Cosmos` Nuget package:
[![Nuget](https://img.shields.io/nuget/v/Sparc.Database.Cosmos?label=Sparc.Database.Cosmos)](https://www.nuget.org/packages/Sparc.Database.Cosmos/)
2. Add the following settings to your `appsettings.json` file:
	```json
	{
      "ConnectionStrings": {
        "CosmosDb": "[your Cosmos DB Connection String]"
	  }
	}
	```

3. Create an Entity Framework `DbContext` class, configuring all root entities as necessary. Example:
    ```csharp
	public class MyAppContext : DbContext
    {
      public MyAppContext(DbContextOptions options) : base(options)
      { }

	  protected override void OnModelCreating(ModelBuilder builder)
	  {
	    builder.Entity<User>().HasPartitionKey(x => x.UserId);
	  }
	}
	```

4. Add the following line of code to your `Startup.cs` file in your Features Project to register the `Sparc.Database.Cosmos` plugin. Pass in the `DbContext` class type you created, the connection string from your `appsettings.json` file, and the name of your database.
    ```csharp
    services.AddCosmos<MyAppContext>(Configuration.GetConnectionString("CosmosDb"), "[your Database Name]");
	```

5. Inject `IRepository<T>` into any feature that needs to load from or save data to the database. All typically necessary database operations exist within this interface. The Cosmos DB Repository you configured will automatically be used.

## More Info

For more information on root entities, or how to use `IRepository<T>`, see the [Sparc.Core documentation](/Sparc.Core).

For an example on using `IRepository<T>` inside a Feature, see the examples in the [Sparc.Features documentation](/Sparc.Features).