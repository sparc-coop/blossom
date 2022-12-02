# Sparc.Kernel

[![Nuget](https://img.shields.io/nuget/v/Sparc.Kernel?label=Sparc.Kernel)](https://www.nuget.org/packages/Sparc.Kernel/)

# Table of contents

- [Let's AddSparcKernel](#lets-addsparckernel)
- [What is a Features Project](#what-is-a-features-project)
- [What is a Feature?](#what-is-a-feature)
    - [Where did the idea of a Feature come from?](#where-did-the-idea-of-a-feature-come-from)
    - [What does a Feature look like?](#what-does-a-feature-look-like)
    - [What are the benefits of using Features?](#what-are-the-benefits-of-using-features)
    - [How do I call a Feature from my UI/Web/Mobile/Desktop project?](#how-do-i-call-a-feature-from-my-uiwebmobiledesktop-project)
    - [Entities and IRepository](#entities-and-irepository)
    - [InMemoryRepository](#inmemoryrepository)
    - [Specification](#specification)
- [Get Started with a Features Project](#get-started-with-a-features-project)
- [Passwordless authentication](#passwordless-authentication)
- [Examples](#examples)
- [FAQ](#faq)
    - [Can I create multiple Features per file, like MVC Controllers do?](#can-i-create-multiple-features-per-file-like-mvc-controllers-do)
    - [Why do you use Records for your Input and Output data?](#why-do-you-use-records-for-your-input-and-output-data)
    - [What if my Feature doesn't have any Input Data?](#what-if-my-feature-doesnt-have-any-input-data)
    - [How do I authenticate my Features and how to use Public Features?](#how-do-i-authenticate-my-features-and-how-to-use-public-features)


## Let's AddSparcKernel
The `Sparc.Kernel` library is the main framework library for the *Features Project* in your Blossom solution. And we can activate it with just two lines of code at your `Program.cs` file

```csharp
    builder.AddSparcKernel(builder.Configuration["WebClientUrl"]);
    ...
    app.UseSparcKernel();
```

Here you will find the steps to create a *Features Project* from the scratch and some of the questions we had to answer to get there.


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

The closer that we can get to this basic formula, the simpler our architecture becomes.

So with that in mind, the basic ingredients of a Feature are the following:

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

### Entities and IRepository

At this point you already noticed we introduced Entities and the IRepository at the code, and that is ok, it should be as simple as this, your entities are normal C# classes and the repository interface has all the deault operations you can expect.

For more information and technical details visit the [Sparc.Core](/Sparc.Core) documentation

### InMemoryRepository

Sparc.Kernel has a built-in [InMemoryRepository](Data/InMemoryRepository.cs), no need to worry about data infrastructure until you really need it, one more way to fasten your development and tests if you want to.
By adding Sparc.Kernel to your Features project you already have an InMemory layer available, you just need to inject the `IRepository<YourEntity>` to your Feature like the `IRepository<Order>` in the example above.

### Specification

Sparc.Kernel has Specification-enabled repositories, which hugely clean up query code in our Features. The Specification pattern encapsulates query logic in its own class, which promotes the reuse of common queries. Some of the main benefits highlighted in the official docs are:

- Keep data access query logic in one place
- Keep data access query logic in the domain layer
- Reuse common queries throughout your application

#### How to use Specifications?

Since Sparc.Kernel already has Specification-enabled repository you can start using it at anytime, we recommend you to create a Queries folder inside your feature's folder

![image](https://user-images.githubusercontent.com/1815134/205227380-20f75626-f2be-40c3-a813-7dfc400476aa.png)

One example of Specification is:
```csharp
using Ardalis.Specification;

namespace YourProject.Features.Licenses.Queries;

public class TransferrableLicenseTypes : Specification<LicenseType>
{
    public TransferrableLicenseTypes()
    {
        Query
            .Include(x => x.Licenses)
            .Where(x => x.IsActive && x.IsVisible);
    }
}
```

and here is how you use it in your feature:
```csharp
using YourProject.Features.Licenses.Queries;

namespace YourProject.Features.Licenses;

public class GetTransferrableLicenses : Feature<List<GetLicensesResponse>>
{
    IRepository<LicenseType> LicenseTypes;
    public GetTransferrableLicenses(IRepository<LicenseType> licenseTypes)
    {
        LicenseTypes = licenseTypes;
    }

    public override async Task<List<GetLicensesResponse>> ExecuteAsync()
    {
        var result = await LicenseTypes.GetAllAsync(new TransferrableLicenseTypes());

        return result.Select(x => new GetLicensesResponse(
            x.Id,
            x.Name))
            .Where(x => x.TotalInStorage > 0)
        .OrderBy(x => x.Name)
        .ToList();
    }
}
```

> You can check more about Specifications [here](http://specification.ardalis.com/)

## Get Started with a Features Project

1. Create a new *ASP.NET Core Empty* project (preferably called *[YourProject]*.Features).
2. Add the `Sparc.Kernel` Nuget package to your newly created project: [![Nuget](https://img.shields.io/nuget/v/Sparc.Kernel?label=Sparc.Kernel)](https://www.nuget.org/packages/Sparc.Kernel/)
3. Add the following setting to your `appsettings.json` file, using the local URL and port of your Web project:
    ```json
    { "WebClientUrl": "https://localhost:7147" }
    ```
    > (Alternatively, you may pass the URL directly as a string in the Startup code below, but we prefer to keep it in `appsettings.json`, since it will change once deployed.)

4. Add the following two lines of code to your `Program.cs` file, in the appropriate methods:

    ```csharp
    public static void Main(string[] args)
    {
        ...
        // Add this line of code
        builder.AddSparcKernel(builder.Configuration["WebClientUrl"]);
        ...
        // Add this line of code
        app.UseSparcKernel();
        ...
    }

    ```

5. Create your Entities and Features. Create a folder structure based on the name of your Entity, you can check out some examples at the [Ibis.Features](https://github.com/sparc-coop/ibis/tree/main/Ibis.Features) project, here is the *Messages* folder with a *Entities* folder inside, where are placed all the related and necessary entities, here is the main [Message class](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Messages/Entities/Message.cs), and last but not least you can also see all the Message related features, such as `DeleteMessage`, `EditMessageTags`, `GetAllMessages`, `HearMessage`, etc.

![image](https://user-images.githubusercontent.com/1815134/204842128-33c30b9b-333b-45e6-82c6-c6bafe8d032a.png)

### And about a entirely new service?

One question you may have along the way is, *and if I need to create a new service what's the best way to do that?*
We're always trying to keep things as clean as possible, so the answer to this question would be to think in your service as a *plugin* where you can just plug and play in your features project. So the suggested steps are

1. Create a *_Plugins* folder at your project and create your services there, we recommend you to implement it from a generic interface, for example, our new service is a translator, and it could be an Azure Translator or using any other provider.
> Here is a real example: Ibis [Plugins folder](https://github.com/sparc-coop/ibis/tree/main/Ibis.Features/_Plugins)

2. Register it on the `Program.cs` file as `builder.Services.AddScoped<ITranslator,AzureTranslator>()`
> Ibis [Program.cs](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Program.cs)

3. Inject it in a feature adding it to the constructor as `ITranslator translator`
> [TranslateMessage Feature](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Messages/TranslateMessage.cs)
---

## Passwordless Authentication

Now Sparc.Kernel comes by default with Passwordless authentication flows, which are the new "more modern" approach to logins. *Microsoft recommends passwordless authentication methods such as Windows Hello, FIDO2 security keys, and the Microsoft Authenticator app because they provide the most secure sign-in experience. Although a user can sign-in using other common methods such as a username and password, passwords should be replaced with more secure authentication methods.* [Microsoft Documentation on Passwordless authentication](https://learn.microsoft.com/en-us/azure/active-directory/authentication/concept-authentication-methods)

> You can check out more about this topic [here](https://www.microsoft.com/en-us/security/business/solutions/passwordless-authentication)

To enable passwordless authentication in your project you need to follow the next steps:

1. At your Features Project add the following settings to your `appsettings.json`
```json
"Passwordless": {
    "Key": "",
    "Issuer": "",
    "Audience": ""
  }
```

2. Add the following to your `Program.cs` file
```csharp
var auth = builder.Services.AddAzureADB2CAuthentication<User>(builder.Configuration); //example considering you're using primarily Azure AD B2C
builder.AddPasswordlessAuthentication<User>(auth);
...
app.UsePasswordlessAuthentication<User>();
```

Yes, that's all.

We're using this in our Ibis project to generate magic links that can be emailed to folks to one-click access rooms they're invited to, without having to manually sign up first or create any sort of password.

Here is a feature that uses our (UserManager Extension)[Authentication/UserManagerPasswordlessExtensions.cs] `CreateMagicSignInLinkAsync`: [InviteUser Feature](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Users/InviteUser.cs)


## Examples

Here are the links to some existing Features projects and features using Blossom

- [Ibis.Features Project](https://github.com/sparc-coop/ibis/tree/main/Ibis.Features)
- [GetRooms Feature](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Rooms/GetRooms.cs)
- [CreateRoom Feature](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Rooms/CreateRoom.cs)
- [DeleteMessage Feature](https://github.com/sparc-coop/ibis/blob/main/Ibis.Features/Messages/DeleteMessage.cs)

## FAQ

### Can I create multiple Features per file, like MVC Controllers do? 

Each feature is self-contained into a single class for a reason. A class is a great container for all of the things a Feature needs:

- A name (the class name)
- An input and output data format (the one-line records above the class are a nice in-file representation of these)
- A set of dependencies (automatically injected into the class constructor by the framework)
- A single function to execute that uses all the other ingredients (the overridden `ExecuteAsync()` function inherited from the `Feature` class)

In addition, one Feature / one class per file ensures that *all* of the logic to execute that Feature resides in a single place. 
This is contrary to the more typical layered approach with separate repositories, managers, controllers, actions, but has proven to be 
a creative and organizational catalyst, as it enables pure focus on the logic that you are working on at the time, rather than having
to hunt all over the code for the stack of functions that are executed.

### Why do you use Records for your Input and Output data?

C# Records are a great way to create a separate data type that is:

- only used once, 
- has no behavior, and 
- are never mutated. 

This happens to be the very definition of a well-constructed DTO (Data Transformation Object).

Since a Feature's Input and Output data classes meet all of these requirements, we can take advantage of a simple one-line construct to create these types, eliminating most of the boilerplate ceremony:

```csharp
public record GetProductsRequest(string SearchTerm, bool ShowDeletedProducts);
...
public record GetAllMessagesResponse(string Language, List<Message> Messages);
```

It is a good practice to return a specific data type for the specific API endpoint you are calling, and to receive a specific data type into 
the API endpoint, rather than using the Entities or shared data types directly. This is the case even when the API needs to return something very close to the Entity itself.
Specific data types per API endpoint enable the following benefits:

- you're protected from accidentally exposing more information than you wanted to (eg. a secure User ID in an entity),
- the core Entities are protected from needing to evolve as the API evolves,
- the shape of each API can evolve separately from all the Entities or other API methods

If you don't like records, you can use classes!

### What if my Feature doesn't have any Input Data?

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

### What if my Feature doesn't have any Output Data?

It is our opinion that *all* Features should have some form of Output Data, so that the projects using this API can know the result of their call. This can be as simple 
as a `bool` or `ActionResult` return if you like, but in most cases there is always something slightly more substantial that can be returned. There is currently 
no Feature type in Sparc.Kernel that returns no Output Data.

### How do I authenticate my Features and how to use Public Features?

All Features inheriting from `Feature<TIn, TOut>` or `Feature<TOut>` are *automatically authenticated* with the `[Authorize]` attribute of ASP.NET Core. This is a 
design decision made on purpose, as most API endpoints in the real world should be private and authenticated.

The simplest way to set up authentication in your Sparc solution is to use one of Sparc's authentication plugins 
([Azure AD B2C](/Sparc.Authentication.AzureADB2C), 
[Active Directory](/Sparc.Authentication.ActiveDirectory), or 
[Self-Hosted](/Sparc.Authentication.SelfHosted)).

However, Sparc.Kernel also contains a non-authenticated Feature option called `PublicFeature`. Public Features are useful for
true public API endpoints (such as a community-accessible list for non-logged-in users). 

To make a feature public, simply inherit from `PublicFeature<TIn, TOut>` or `PublicFeature<TOut>` rather than `Feature<TIn, TOut>` or `Feature<TOut>`.

Public Features are also useful in the initial stages of building your app, before any authentication is set up. 
