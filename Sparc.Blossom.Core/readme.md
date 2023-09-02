# Sparc.Blossom.Core

[![Nuget](https://img.shields.io/nuget/v/Sparc.Blossom.Core?label=Sparc.Blossom.Core)](https://www.nuget.org/packages/Sparc.Blossom.Core/)

The `Sparc.Blossom.Core` library contains a few shared classes and interfaces that are used by many libraries in Sparc.Kernel.

This readme will serve as an architectural guide for best practices on Entities, Roots, and Repositories, and how to design them the Blossom way.

# Table of contents
- [The Blossom Core Architecture](#the-blossom-core-architecture)
	- [Entities vs. Database Schemas](#entities-vs-database-schemas)
	- [Write your Entities as Normal C# Classes](#write-your-entities-as-normal-c#-classes)
	- [Features and Repositories](#features-and-repositories)
	- [IFileRepository](#ifilerepository)
	- [ISqlRepository](#isqlrepository)

## The Sparc.Blossom Core Architecture

### Entities vs. Database Schemas

Almost all applications need some form of data persistence. When we use a typical 3-layer architecture, we tend to let the database schema define the entire design
of every entity and behavior throughout the app. Many developers design the entire database schema first, and only then do they start thinking about the application itself.

With Blossom, we want to challenge that assumption.

We want you to think of the database as just a detail -- a detail that doesn't even have to be decided upon until the very end.

We challenge you to try the following instead:

- write your entities as normal C# classes, free of all the constraints and nuances of a relational or NoSQL database
- identify your core entities (i.e. those at the top of the hierarchy, not dependent on others. `Order`, not `OrderDetail`). 
- write your entity's behaviors as methods within those C# classes, mostly within your Root Entities

In other words, just write the application first, and get it working. Worry about the persistence later. It will be a plugin you can add and configure in just a few minutes.

And try not to think about relating your entity design to a database. No matter what hierarchy your classes take, there will be a way to persist them.

### Write your Entities as Normal C# Classes

If you're building an order processing engine, you certainly need an `Order` class. So let's start there. 

An Order is at the "top" of the hierarchy, so we'll make it a Root class as well. This will make it one of our Root Entities, and will make it a hub for repository access. The `Root` class in Sparc.Kernel is a simple class that defines the Id property and type for the entity (in this case, a string).

We'll also add a few simple properties that make sense for an Order.

```csharp
public class Order : Root<string>
{
	public Order()
	{
		Id = Guid.NewGuid().ToString();
		DateCreated = DateTime.UtcNow;
	}

	public List<OrderDetail> Items { get; set; } = new();
	public DateTime DateCreated { get; set; }
	public DateTime? DatePlaced { get; set;}
}
```

We'll also go ahead and create our `OrderDetail` class. An `OrderDetail` never exists without an `Order`, so it won't be a Root Entity -- just a normal class:

```csharp
public class OrderDetail
{
	public OrderDetail(string itemId, int quantity, decimal price)
	{
		ItemId = itemId;
		Quantity = quantity;
		Price = price;
	}
	
	public string ItemId { get; set; }
	public int Quantity { get; set; }
	public decimal Price { get; set; }
}
```

Now, how do we add an item to an order? We're probably used to having some utility function that does this for us, or a repository method. But these are just classes. How would we do it with normal C# classes, if we had nothing else? 

We would simply add a method to the Order entity. So let's do that. Let's also add a method to place the order while we're in here:

```csharp
public class Order : Root<string>
{
	public Order()
	{
		Id = Guid.NewGuid().ToString();
		DateCreated = DateTime.UtcNow;
	}

	public List<OrderDetail> Items { get; set; } = new();
	public DateTime DateCreated { get; set; }
	public DateTime? DatePlaced { get; set;}

	public int AddItem(string id, int quantity, decimal price)
	{ 
		var item = Items.FirstOrDefault(x => x.ItemId == id);
		if (item != null)
			existing.Quantity += quantity;
		else
		{
			item = new(id, quantity, price);
			Items.Add(item);
		}
		
		return item.Quantity;
	}

	public void Submit() => DatePlaced = DateTime.UtcNow;
}
```

Notice there's no persistence, no repositories, no thought of a database. Just pure classes and their behavior. We're back to the essence of object-oriented programming!

### Features and Repositories

So what's left in the chain of events? 

Well, somehow we still have to persist all this order data beyond a single user's session. And that's where a database comes to the rescue. 

But we can recognize that *this one thing* -- persistence of data -- is a database's main job! Not app design, not data validation, not app logic, nor any of 
the other app-like things we have burdened most modern databases with. 

It's just this: get a piece of data, and save the result once we do something new to it, so somebody else can get it later. 

We have to let the app do *everything else*. We have to design the app so it can fully do everything it is designed to do, without even needing a database at all.

### IFileRepository

Following the same idea we have the IFileRepository, so you'll have it available in case you need to implement your own or use one of our plugins, like for example the [![Nuget](https://img.shields.io/nuget/v/Sparc.Storage.Azure?label=Sparc.Storage.Azure)](https://www.nuget.org/packages/Sparc.Storage.Azure/). 

In case you want to check how it implements the interface here is the [AzureBlobRepository.cs](/Sparc.Storage.Azure/AzureBlobRepository.cs)

### ISqlRepository

This one is an example of when you already have your infrastructure defined or at least an idea that your project will use a SQL database, for example. So here we extended the [IRepository](Data/IRepository.cs) to have specific SQL operations (`FromSqlAsync`) check the [ISqlRepository](Data/ISqlRepository.cs) and one of its implementations at [SqlServerRepository.cs](/Sparc.Database.SqlServer/SqlServerRepository.cs)
