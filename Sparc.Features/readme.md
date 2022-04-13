# Sparc.Features

The `Sparc.Features` library is the main framework library for the *Features Project* in your Sparc solution.

## What is a Features Project?

A *Features Project* is the core of your application, and arguably *is your entire application*. The UI platforms can be viewed as a plugin that enables a visible interface to access this project.

A *Features Project* is also the main back end API for your application. 

This project should ideally strive to contain *all* of the application's logic, including:

- entities, 
- operations on those entities,
- the entire API surface (including URL routes, dependencies, input and output data classes) 
- all plugins (persistence, authentication, notification, etc.)

## What is a Feature?

A *Feature* is a single operation that your app can perform, along with all of the necessary dependencies around that operation (persistence, authentication, notification, etc.). 

### Where did the idea of a Feature come from?

Almost all programming in the world can be abstracted down to the following formula:

```
    In -> Modify() -> Out
```

Every function, every group of functions, every program, every project, every solution, is a long linear chain of this basic formula.

So, the closer that we can get to this basic formula, the simpler our architecture becomes.

With that in mind, the basic ingredients of a Feature are the following:

#### In:

- The *Name* of the Feature
- The *Input Data* that the Feature needs 
- The *Dependencies* that the Feature uses to do its job

#### Modify:

- The *Logic* that the Feature executes, using the Data and Dependencies to produce an Output

#### Out:

- The *Result* that the Feature spits out at the end of its job

### What does a Feature look like?

```csharp
// This is the form of your Input Data
public record GetOrderRequest(string CustomerId, string PurchaseOrderNumber);
// This is the form of your Output Data
public record GetOrderResponse(string OrderId, List<OrderDetail> Lines, decimal Tax, decimal Shipping);

// Inherit your class from Feature<InputType, OutputType> to enable all the goodness.
// Name your Feature well. It will become the permanent URL of your API (i.e. /api/GetOrder)
public class GetOrder : Feature<GetOrderRequest, GetOrderResponse>
{
   // Inject your Dependencies into the constructor
   public GetOrder(IRepository<Order> orders) => Orders = orders;

   // Receive the Input Data as a parameter, use it in the body of the function, 
   // and return the Output Data as the result
   public override async Task<GetOrderResponse> ExecuteAsync(GetOrderRequest request)
   {
       var order = Orders.Query.FirstOrDefault(o => 
            o.CustomerId == request.CustomerId 
         && o.PurchaseOrderNumber == request.PurchaseOrderNumber);
       return new(order.Id, order.Lines, order.CalculateTax(), order.CalculateShipping());
   }
}
```


### What are the benefits of using Features?

Sparc.Kernel automatically turns every Feature you write into a separate API endpoint, with its own URL and full authentication support, with no additional configuration needed on your part. 

When the project containing the example Feature above is built, Sparc.Kernel automatically creates the following:

- A protected API endpoint at `/api/GetOrder`
- An auto-generated client class with a method for each Feature, which automatically calls the API at the correct URL and with the correct authentication headers: 
```csharp
public async Task<GetOrderResponse> GetOrderAsync(GetOrderRequest request);
```

### How do I call a Feature from my UI/Web/Mobile/Desktop project?

1. Inject the auto-generated Api class into your Blazor page/component (you can also do this once for the entire application in the Imports.razor file):
```razor
@inject PointOfSaleApi Api
```
2. Call the appropriate method on the Api class (note: it will be named `[FeatureName]Async`):
```cs
var request = new(customerId, poNum);
var order = await Api.GetOrderAsync(request);
```

## Get Started with a Features Project

1. Create a new *ASP.NET Core Empty* project (preferably called *[YourProject]*.Features).
2. Add the `Sparc.Features` Nuget package to your newly created project: [![Nuget](https://img.shields.io/nuget/v/Sparc.Features?label=Sparc.Features)](https://www.nuget.org/packages/Sparc.Features/)
3. Add the following setting to your `appsettings.json` file, using the local URL and port of your Web project:
```json
{ "WebClientUrl": "https://localhost:7147"  }
```
> (Alternatively, you may pass the URL directly as a string in the Startup code below, but we prefer to keep it in `appsettings.json`, since it will change once deployed.)

4. Add the following two lines of code to your `Startup.cs` file, in the appropriate methods:

```csharp
public void ConfigureServices(IServiceCollection services)
{
    // Add this line of code
    services.Sparcify<Startup>(Configuration["WebClientUrl"]);
}

public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
{
    // Add this line of code
    app.Sparcify<Startup>(env);
}
```

5. Create your Entities and Features.

---

# FAQ

## Can I create multiple Features per file, like MVC Controllers do? 

Each feature is self-contained into a single class for a reason. A class is a great container for all of the things a Feature needs:

- A name (the class name)
- An input and output data format (the one-line records above the class are a nice in-file representation of these)
- A set of dependencies (automatically injected into the class constructor by the framework)
- A single function to execute that uses all the other ingredients (the overridden `ExecuteAsync()` function inherited from the `Feature` class)

In addition, one Feature / one class per file ensures that *all* of the logic to execute that Feature resides in a single place. 
This is contrary to the more typical layered approach with separate repositories, managers, controllers, actions, but has proven to be 
a creative and organizational catalyst, as it enables pure focus on the logic that you are working on at the time, rather than having
to hunt all over the code for the stack of functions that are executed.

## Why do you use Records for your Input and Output data?

C# Records are a great way to create a separate data type that is:

- only used once, 
- has no behavior, and 
- are never mutated. 

This happens to be the very definition of a well-constructed DTO (Data Transformation Object).

Since a Feature's Input and Output data classes meet all of these requirements, we can take advantage of a simple one-line construct to create these types, eliminating most of the boilerplate ceremony:

```csharp
public record GetProductsRequest(string SearchTerm, bool ShowDeletedProducts);
```

It is a good practice to return a specific data type for the specific API endpoint you are calling, and to receive a specific data type into 
the API endpoint, rather than using the Entities or shared data types directly. This is the case even when the API needs to return something very close to the Entity itself.
Specific data types per API endpoint enable the following benefits:

- you're protected from accidentally exposing more information than you wanted to (eg. a secure User ID in an entity),
- the core Entities are protected from needing to evolve as the API evolves,
- the shape of each API can evolve separately from all the Entities or other API methods


## What if my Feature doesn't have any Input Data?

If you need to call an API method that only returns Output Data without any Input Data required, inherit your Feature from `Feature<[your output data type]>` 
instead. This enables the following changes to your Feature:

- The `ExecuteAsync` function has no input parameters:

```csharp
public override async Task<GetAllOrdersResponse> ExecuteAsync() {}
```

- The client API will also automatically take no parameters:

```csharp
var orders = await Api.GetAllOrdersAsync();
```

## What if my Feature doesn't have any Output Data?

It is our opinion that *all* Features should have some form of Output Data, so that the projects using this API can know the result of their call. This can be as simple 
as a `bool` or `ActionResult` return if you like, but in most cases there is always something slightly more substantial that can be returned. There is currently 
no Feature type in Sparc.Kernel that returns no Output Data.

## How do I authenticate my Features?

All Features inheriting from `Feature<TIn, TOut>` or `Feature<TOut>` are *automatically authenticated* with the `[Authorize]` attribute of ASP.NET Core. This is a 
design decision made on purpose, as most API endpoints in the real world should be private and authenticated.

The simplest way to set up authentication in your Sparc solution is to use one of Sparc's authentication plugins 
([Azure AD B2C](https://github.com/sparc-coop/Sparc.Kernel/tree/feature/documentation/Sparc.Authentication.AzureADB2C), 
[Active Directory](https://github.com/sparc-coop/Sparc.Kernel/tree/feature/documentation/Sparc.Authentication.ActiveDirectory), or 
[Self-Hosted](https://github.com/sparc-coop/Sparc.Kernel/tree/feature/documentation/Sparc.Authentication.SelfHosted)).

However, Sparc.Kernel also contains a non-authenticated Feature option called `PublicFeature`. Public Features are useful for
true public API endpoints (such as a community-accessible list for non-logged-in users). 

To make a feature public, simply inherit from `PublicFeature<TIn, TOut>` or `PublicFeature<TOut>` rather than `Feature<TIn, TOut>` or `Feature<TOut>`.

Public Features are also useful in the initial stages of building your app, before any authentication is set up. 