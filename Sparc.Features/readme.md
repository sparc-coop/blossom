# Sparc.Features

The `Sparc.Features` library is the main framework library for the **Sparc Features** project in your solution.

# What is a Sparc Features project?

A Sparc Features project serves as the main back end API for your application. This project contains *all* of the application's back end logic.

## What is a Feature?

A *Feature* is an operation that your app can perform, along with all of the necessary dependencies around that operation (including database retrieval and persistence and other plugins). 

## If I use Features, what does the Sparc framework do for me?

The Sparc framework automatically turns every Feature you write into a separate API endpoint, with its own URL and full authentication support, without any additional configuration needed on your part. 

Additionally, the framework auto-generates a corresponding C# client method so that the UI/Platform classes can call it directly, without needing to rely on `HttpClient` or a custom implementation. 

This means that every single API call in your application can be fully executed with a single line of code. Example:

```var profile = await Api.GetUserProfileAsync(UserId);```

## How do I set up a Features project?

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

# Architectural Guidance

## Where did the idea of a Feature come from?

Almost all programming in the world can be abstracted down to the following formula:

```
    In -> Modify() -> Out
```

Every function, every group of functions, every program, every project, every solution, is a long linear chain of this basic formula.

So, the closer that we can get to this basic formula, the simpler our architecture becomes.

With that in mind, the basic ingredients of a Feature are the following:

### In:

- The *Name* of the Feature
- The *Input Data* that the Feature needs 
- The *Dependencies* that the Feature uses to do its job

### Modify:

- The *Logic* that the Feature executes, using the Data and Dependencies to produce an Output

### Out:

- The *Result* that the Feature spits out at the end of its job

## What does a Feature look like?

```csharp
// This is the form of your Input Data
public record GetOrderRequest(string CustomerId, string PurchaseOrderNumber);
// This is the form of your Output Data
public record GetOrderResponse(string OrderId, List<OrderDetail> Lines, decimal Tax, decimal Shipping);

// Name your Feature well. It will become the permanent URL of your API.
public class GetOrder : Feature<GetOrderRequest, GetOrderResponse>
{
   // Inject your Dependencies into the constructor
   public GetOrder(IRepository<Order> orders) => Orders = orders;

   // Write your logic in the overridden ExecuteAsync function
   // Receive the Input Data as a parameter
   // Return the Output Data as the result
   public override async Task<GetOrderResponse> ExecuteAsync(GetOrderRequest request)
   {
       var order = Orders.Query.FirstOrDefault(o => o.CustomerId == request.CustomerId && o.PurchaseOrderNumber == request.PurchaseOrderNumber);
       return new(order.Id, order.Lines, order.CalculateTax(), order.CalculateShipping());
   }
}
```

Once the project containing this Feature is built, the framework will automatically produce the following:

- A protected API endpoint at `/api/GetOrder`
- A method in an auto-generated client class which automatically calls the API at the correct URL and with the correct authentication headers: 
```csharp
public async Task<GetOrderResponse> GetOrderAsync(GetOrderRequest request)
```

## How do I call a Feature from my UI/Web project?

1. Inject the auto-generated Api class into your Blazor page/component (you can also do this once for the entire application in the Imports.razor file):
```razor
@inject PointOfSaleApi Api
```
2. Call the appropriate method on the Api class (note: it will be named `[FeatureName]Async`):
```cs
var request = new(customerId, poNum);
var order = await Api.GetOrderAsync(request);
```
## Can I create multiple Features per file, like MVC Controllers do? 

## Why do you use Records for your Input and Output data?

## What if my Feature doesn't have any Input Data?

## What if my Feature doesn't have any Output Data?

