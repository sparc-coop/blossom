[![MIT License](https://img.shields.io/github/license/sparc-coop/Sparc.Kernel)](https://github.com/sparc-coop/Sparc.Kernel/blob/main/LICENSE)

<picture>
  <source media="(prefers-color-scheme: dark)" srcset="Sparc.Core/Files/blossom_logo_white.png">
  <source media="(prefers-color-scheme: light)" srcset="Sparc.Core/Files/blossom_logo_black.png">
  <img alt="Shows an illustrated sun in light mode and a moon with stars in dark mode." src="Sparc.Core/Files/blossom_logo_black.png">
</picture>

# Table of contents

- :blossom: [What is Blossom?](#what-is-blossom)
  - [OK, what is it really?](#ok-what-is-it-really)
  - [How is a Blossom solution structured?](#how-is-a-blossom-solution-structured)
- :rocket: [Get Started with Blossom](#get-started-with-blossom)
  - [Step 1. Create a Blossom Solution with a Features and Web Project](#step-1-create-a-blossom-solution-with-a-features-and-web-project)
  - [Step 2. Write your app](#step-2-write-your-app)
  - [Step 3. Add Sparc plugins as you need them](#step-3-add-sparc-plugins-as-you-need-them)
    - [Database](#database)
    - [Authentication](#authentication)
    - [Notifications](#notifications)
    - [Storage](#storage)
  - [Step 4. Deploy your solution](#step-4-deploy-your-solution)
- :notebook: [Documentation](#documentation)
- :computer: [Examples / Templates](#examples--templates)
- :hammer_and_wrench: [Built With](#built-with)
- :handshake: [Contributing](#contributing)



# What is Blossom?

**Blossom** is an opinionated framework-of-a-framework for .NET 7.0 Web, Mobile, and Desktop development using a single shared codebase (C# and Blazor).

## OK, what is it really?

**Blossom** is a hand-crafted framework full of architectural conventions that attempt to remove as much of the boilerplate connective tissue as possible. 

Its chief aim is to remove all the drivel it possibly can, so that you may focus more of your creative programming energy on what really matters for your app: 

- the logic of the back end, and 
- the presentation of the front end.

Everything else should be secondary.

## How is a Blossom solution structured?

A typical Blossom solution has three main components: 

- one **Features** project. This project is the main back end API for the application, and contains the vast majority of the application's logic.
- one **UI** project. This project is the shared front end for Web, Mobile, and Desktop, using Blazor as its underlying architecture. 
- *(optional)* one or more **Platforms** projects, if you're deploying to multiple platforms. These projects are the deployable units for Web (using Sparc.Platforms.Web) and Android/iOS/Mac/Windows (using Sparc.Platforms.Maui). They are typically very small projects with only platform-specific startup code and platform overrides. The vast majority of the UI code should still exist in the shared UI project.

# Get Started with Blossom

## Step 1. Create a Blossom Solution with a Features and Web Project

1. **Features Project:** Create a new .NET 7.0 solution with an *ASP.NET Core Empty* project (preferably called *[YourProject]*.Features).
> Follow the [Sparc.Kernel documentation](/Sparc.Kernel#get-started-with-a-features-project) for setup.
[![Nuget](https://img.shields.io/nuget/v/Sparc.Kernel?label=Sparc.Kernel)](https://www.nuget.org/packages/Sparc.Kernel/)


2. **Web Project:** Add a *Blazor Web Assembly App* project to your solution (preferably called *[YourProject]*.Web). 
> Follow the [Sparc.Blossom documentation](/Sparc.Blossom) for setup.
[![Nuget](https://img.shields.io/nuget/v/Sparc.Blossom?label=Sparc.Blossom)](https://www.nuget.org/packages/Sparc.Blossom/)

### Step 1a *(optional, if you're targeting mobile/desktop platforms)*: Add a Shared UI project and a MAUI Desktop/Mobile project

1. **Shared UI Project:** Add a *Razor Class Library* project to your solution (preferably called *[YourProject]*.UI).

2. **Mobile/Desktop Project:** Add a *.NET MAUI Blazor App* project to your solution (preferably called *[YourProject]*.Maui).
> Follow the [Sparc.Blossom documentation](/Sparc.Blossom) for setup.
[![Nuget](https://img.shields.io/nuget/v/Sparc.Blossom?label=Sparc.Blossom)](https://www.nuget.org/packages/Sparc.Blossom/)


## Step 2. Write your app

1. Create your base entity classes in the Sparc.Features project. Entities are the core classes that your app uses. 
> Examples of entities are `Order`, `User`, `Product`, `OrderDetail`, etc. See [Sparc.Kernel documentation](/Sparc.Kernel) for architectural guidance.

2. Create a Feature for each "feature" that your app needs. Features are operations that your app can perform and all of the necessary dependencies around that operation (including database retrieval and persistence and other plugins). Each Feature automatically becomes a separate API endpoint.
> Examples of Features are `GetOrder`, `SaveOrder`, `GetUserProfile`, etc. See [Sparc.Kernel documentation](/Sparc.Kernel) for architectural guidance.

3. Create a Blazor Page/Component for each UI Page/Component that your app needs, and place them in the UI project (or Web for web-only projects).
> Examples of Pages are `Orders/Index.razor`, `ProductDetail.razor`, `Profile.razor`, etc. Examples of Components are `ProductSummary.razor`, `OrdersList.razor`, `Avatar.razor`, etc. See [Sparc.Blossom documentation](/Sparc.Blossom) for architectural guidance.

4. Call your Features from your UI, using the auto-generated `Api` class that the framework creates for you.
> An `Api` class is automatically regenerated on each build using the `swagger.json` file from your Features project, which is *also* automatically regenerated on each build. This class is typically called *[YourProject]Api*, eg. `PointOfSaleApi`, and it automatically contains a method for every Feature you've implemented, eg. `await Api.GetOrdersAsync(CustomerId)`.

5. Run the Features project and appropriate Platform project (normally `Platforms.Web` for fastest development) locally to test and debug your application.
 
## Step 3. Add Sparc plugins as you need them

### Database

The Sparc.Kernel library comes with a default in-memory implementation of `IRepository`, so you likely don't even need to set up a database in the initial stages of development. Just inject `IRepository<Entity>` everywhere as needed, and all data will be loaded from/saved to local memory.

When you are ready to add a real database, simply add the appropriate NuGet package to the `Features` project and configure it in the Startup class. 

Blossom currently offers libraries for two database providers:

- Azure Cosmos DB: [Sparc.Database.Cosmos](/Sparc.Database.Cosmos)
[![Nuget](https://img.shields.io/nuget/v/Sparc.Database.Cosmos?label=Sparc.Database.Cosmos)](https://www.nuget.org/packages/Sparc.Database.Cosmos/)

- SQL Server / Azure SQL: [Sparc.Database.SqlServer](/Sparc.Database.SqlServer)
[![Nuget](https://img.shields.io/nuget/v/Sparc.Database.SqlServer?label=Sparc.Database.SqlServer)](https://www.nuget.org/packages/Sparc.Database.SqlServer/)

You may also implement your own instance of `IRepository<T>` if you desire a custom implementation or need a different database provider.

### Authentication 

All Features by default require some form of authentication, as most real-world API endpoints are private, not public. However, Blossom includes a feature type called `PublicFeature` which opens up anonymous access to the feature. If you wish to defer user authentication to a later point in the development of your app, you can simply use `PublicFeature` for all features until you're ready.

When you're ready, simply add the appropriate NuGet package to the `Features` project and configure it per the documentation. 

Blossom includes three main options for authentication:

- Azure AD B2C (good for secure OAuth2.0 consumer apps): 
[Sparc.Authentication.AzureADB2C](/Sparc.Authentication.AzureADB2C) 
[![Nuget](https://img.shields.io/nuget/v/Sparc.Authentication.AzureADB2C?label=Sparc.Authentication.AzureADB2C)](https://www.nuget.org/packages/Sparc.Authentication.AzureADB2C/)

- Active Directory (good for internal apps): [Sparc.Authentication.ActiveDirectory](/Sparc.Authentication.ActiveDirectory)
[![Nuget](https://img.shields.io/nuget/v/Sparc.Authentication.ActiveDirectory?label=Sparc.Authentication.ActiveDirectory)](https://www.nuget.org/packages/Sparc.Authentication.ActiveDirectory/)


- Self-Hosted Identity Server (good for custom authentication flows): [Sparc.Authentication.SelfHosted](/Sparc.Authentication.SelfHosted)
[![Nuget](https://img.shields.io/nuget/v/Sparc.Authentication.SelfHosted?label=Sparc.Authentication.SelfHosted)](https://www.nuget.org/packages/Sparc.Authentication.SelfHosted/)

### Notifications

To send emails, text messages, and push notifications, Blossom offers two libraries:

- Twilio (for emails and SMS messages): [Sparc.Notifications.Twilio](/Sparc.Notifications.Twilio)
[![Nuget](https://img.shields.io/nuget/v/Sparc.Notifications.Twilio?label=Sparc.Notifications.Twilio)](https://www.nuget.org/packages/Sparc.Notifications.Twilio/)

- Azure Notification Hub (for web and mobile push notifications): [Sparc.Notifications.Azure](/Sparc.Notifications.Azure)
[![Nuget](https://img.shields.io/nuget/v/Sparc.Notifications.Azure?label=Sparc.Notifications.Azure)](https://www.nuget.org/packages/Sparc.Notifications.Azure/)

### Storage

To upload and download blob files using a modern storage solution, Blossom offers:

- Azure Storage Plugin: [Sparc.Storage.Azure](https://www.nuget.org/packages/Sparc.Storage.Azure)


## Step 4. Deploy your solution

1. Deploy the Features project to any .NET 7.0 ASP.NET Core-compatible host (eg. Azure App Services).
2. Deploy the Web Platform project to any Web Assembly-compatible host (eg. Azure App Services).
3. Deploy the MAUI Platform project to Google, Apple, and Windows stores, or as desired.

# Documentation

Learn more about how to use Blossom by visiting the source folder for each package (linked above). 
Each package contains its own readme for installing and getting started.

# Examples / Templates

Blossom is the architecture for all of [Sparc Cooperative](https://www.sparc.coop)'s ongoing projects, including:

- [Law of 100](https://github.com/sparc-coop/law-of-100) (Features, Web, Authentication.AzureADB2C, Database.Cosmos, Notifications.Azure)
- [Kodekit](https://github.com/sparc-coop/kodekit) (Features, Web, Authentication.AzureADB2C, Database.Cosmos)
- [Ibis](https://github.com/sparc-coop/ibis) (Features, Web, Authentication.AzureADB2C, Notifications.Twilio, Storage.Azure, Database.Cosmos)

# Built With

Blossom is built on the shoulders of giants:

- [Ardalis.ApiEndpoints](https://github.com/ardalis/ApiEndpoints) by [@ardalis](https://twitter.com/ardalis)
- [IdentityServer](https://github.com/IdentityServer) by [Duende](https://twitter.com/DuendeIdentity)
- [Swagger](https://swagger.io/)
- [NSwag](https://github.com/RicoSuter/NSwag) by [@RicoSuter](https://twitter.com/RicoSuter)
- [Blazored LocalStorage](https://github.com/Blazored/LocalStorage) by [@chris_sainty](https://twitter.com/chris_sainty)
- [Bogus](https://github.com/bchavez/Bogus) by [@bchavez](https://twitter.com/bchavez)
- [.NET Core](https://github.com/dotnet/core), [ASP.NET Core](https://github.com/dotnet/aspnetcore), [MAUI](https://github.com/dotnet/maui), [Entity Framework](https://github.com/dotnet/efcore), [Cosmos DB](https://github.com/Azure/azure-cosmos-dotnet-v3), and many more from [@microsoft](https://twitter.com/Microsoft)

# Contributing

Please read our contributing guidelines [here](./CONTRIBUTING.md). 
