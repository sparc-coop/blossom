# Sparc.UI

[![Nuget](https://img.shields.io/nuget/v/Sparc.UI?label=Sparc.UI)](https://www.nuget.org/packages/Sparc.UI/)

The `Sparc.UI` library is the main framework library for the *UI Project* in your Sparc solution.

## What is a UI Project?

A *UI Project* is where you put all shared Blazor Pages and Components for your Web and MAUI projects to use.

If your project is multi-platform (i.e. web + desktop and/or mobile), this project should exist so that you are only writing the UI codebase once and sharing it across every platform.

All UI responsiveness is handled through CSS media queries, as a mobile web application would handle it.

In short: Write your UI as a mobile-responsive web application, and you get Desktop & Mobile apps "for free".

## Get Started with a UI Project

1. Add a *Razor Class Library* project to your solution (preferably called *[YourProject]*.UI).
2. Reference your UI Project from your Web and MAUI Projects (as a Project Reference).
3. Write your user interface within your UI Project as Blazor Pages and Components.

## Connect your UI to your Features

One of the best aspects of Sparc.Kernel is its ability to auto-generate a client method for every Feature you write, with zero configuration.

Every time your solution is built, Sparc.Kernel automatically creates the following, for every feature:

- An entire API surface for all of your Features (eg. `/api/DoSomething`)
- An auto-generated client class with a method for each Feature, which automatically calls the API at the correct URL and with the correct authentication headers: 
    ```csharp
    public async Task<GetOrderResponse> DoSomethingAsync(GetOrderRequest request);
    ```

To set this up, you need to point your UI Project to the `swagger.json` file in your Features Project, and configure it as an OpenAPI reference:

1. Right-click your UI Project -> Add Connected Service -> Add Service Reference -> OpenAPI
2. Choose the "File" Radio button, and navigate and select the `swagger.json` file generated inside your Features Project.
3. Type in any namespace and class name you desire for your client-side Api class.
4. Click OK. The Api class will now be generated for you, and will regenerate automatically on every new build.
	> If you ever need to manually regenerate this class, simply open the Connected Service -> Regenerate.

To use this Api class in your Blazor components:

1. Inject the Api class into your app (preferably in the `_Imports.razor` global file):
	```razor
	@inject PointOfSaleApi Api
	```
2. Use the Api class throughout your application.
	```razor
	var orders = await Api.GetAllOrdersAsync();
	```

