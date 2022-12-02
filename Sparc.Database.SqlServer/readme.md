# Sparc.Database.SqlServer

[![Nuget](https://img.shields.io/nuget/v/Sparc.Database.SqlServer?label=Sparc.Database.SqlServer)](https://www.nuget.org/packages/Sparc.Database.SqlServer/)

The `Sparc.Database.SqlServer` plugin is primarily an implementation of `IRepository<T>` that uses [any Sql Server DB](https://docs.microsoft.com/en-us/azure/azure-sql/) via Entity Framework for all of its persistence operations.

Add this plugin to your Features Project if you'd like to use a Sql Server DB as your app's database provider.

## Get Started with Sparc.Database.SqlServer

In Your Features Project:

1. Add the `Sparc.Database.SqlServer` Nuget package:
[![Nuget](https://img.shields.io/nuget/v/Sparc.Database.SqlServer?label=Sparc.Database.SqlServer)](https://www.nuget.org/packages/Sparc.Database.SqlServer/)
2. Add the following settings to your `appsettings.json` file:
	```json
	{
      "ConnectionStrings": {
        "Database": "[your SqlServer DB Connection String]"
	  }
	}
	```

3. Create a `SparcContext` class, configuring all root entities as necessary. Example:
    ```csharp
	public class MyAppContext : SparcContext
    {
      public virtual DbSet<User> Users => Set<User>();
      public MyAppContext(DbContextOptions options, Publisher publisher) : base(options, publisher)
      { }

	  protected override void OnModelCreating(ModelBuilder builder)
	  {
	    builder.Entity<User>().ToTable("Users");
	  }
	}
	```

4. Add the following line of code to your `Program.cs` file in your Features Project to register the `Sparc.Database.SqlServer` plugin. Pass in the `SparcContext` class type you created, the connection string from your `appsettings.json` file, and the name of your database.
    ```csharp
    builder.Services.AddSqlServer<MyAppContext>(builder.Configuration.GetConnectionString("Database"));
	```

5. Inject `IRepository<T>` into any feature that needs to load from or save data to the database. All typically necessary database operations exist within this interface. The SqlServer DB Repository you configured will automatically be used.

## More Info

For more information on root entities, or how to use `IRepository<T>`, see the [Sparc.Core documentation](/Sparc.Core).

For an example on using `IRepository<T>` inside a Feature, see the examples in the [Sparc.Kernel documentation](/Sparc.Kernel).
