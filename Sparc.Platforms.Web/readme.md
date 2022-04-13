# Sparc.Platforms.Web

The `Sparc.Platforms.Web` library is the main framework library for the *Web Project* in your Sparc solution.

## What is a Web Project?

A *Web Project* is the project that you intend to deploy to Web platforms for use in a web browser.

If your project is multi-platform (i.e. web + desktop and/or mobile), this project should ideally contain only **startup** and **platform-specific** code.

If your project is web-only, the *Web Project* can also contain all of the UI components and pages for your project. 
Otherwise, the UI components and pages should go into a shared [Sparc.UI](/Sparc.UI) project, so 
that the mobile/desktop (MAUI) project can use the same UI components.

A *Web Project* is currently driven by Blazor Web Assembly. This way it acts as a self-contained application similar to mobile and desktop.

## Get Started with a Web Project

1. Create a new *Blazor Web Assembly App* project, preferably called *[Your Project]*.Web.
2. Add the `Sparc.Platforms.Web` Nuget package to your newly created project: 
[![Nuget](https://img.shields.io/nuget/v/Sparc.Platforms.Web?label=Sparc.Platforms.Web)](https://www.nuget.org/packages/Sparc.Platforms.Web/)
3. Add the following setting to your `wwwroot/appsettings.json` file, using the local URL and port of your Features Project:
```json
{ "ApiUrl": "https://localhost:7001"  }
```
4. Add the following line of code to your `Startup.cs` file, in the appropriate method:

```csharp
    public static async Task Main(string[] args)
    {
        var builder = WebAssemblyHostBuilder.CreateDefault(args);

        // Add this line of code
        builder.Sparcify();
    }
```
5. Modify the `App.razor` file at the root of your Web Project, fully replacing its contents with the following:
```html
<Sparc.Platforms.Web.SparcApp 
        MainLayout="typeof(MainLayout)" 
        Startup="typeof(Program)" />
```
6. Write your app.
    a. *(Web-only projects)* Write your UI pages and components directly in the Web Project, using guidance from the [Sparc.UI documentation](/Sparc.UI).
    b. *(Multi-platform projects)* Create a [Sparc.UI](/Sparc.UI) project, reference it from your Web Project, and write your UI components within the UI project.

## Run and Debug a Web Project Locally

Normally, a Web Project should be run in parallel with your Features Project, so that the local API can be accessed directly. 

Our favorite way to set this up with minimal issues is the following:

1. Right-click your Web Project -> Set as Startup Project.
2. Ensure your Web Startup settings are set correctly (i.e. IIS Express or the Project Name itself), and take note of the assigned ports in `launchsettings.json` for the selected startup path.
3. Right-click your Features Project -> Set as Startup Project.
4. Ensure your Features Startup settings are set correctly (i.e. IIS Express or the Project Name itself), and take note of the assigned ports in `launchsettings.json` for the selected startup path.
5. Ensure that the `WebClientUrl` in your Features project and `ApiUrl` in your Web project point to the correct local ports.
6. Right-click your Solution -> Set Startup Projects.
7. Set your Features and Web projects to "Start" to enable local debugging and Hot Reload.
8. Set all other projects to "None".
9. Run your solution. The projects will each run according to the settings you chose in steps #2 and #4, and full debugging + Hot Reload will be enabled for both projects.

## Deploy your Web Project to the Web

Your *Web Project* is directly deployable to any Web Assembly-compatible host (such as Azure App Service). Simply use the normal publishing procedures
to publish your project like any other Web project.

Once deployed, ensure that the `ApiUrl` and `WebClientUrl` settings are updated to match the live URLs.

## FAQ

### How do I create Web platform-specific code in multi-platform projects?

A Web Project can override any behavior from your UI project in two ways:

#### Override Behavior in Classes (best for logic and C# code)

1. Create an interface in your *UI Project* that contains the methods that need to be overridden (eg. `IEmailService`).
2. Use this interface in all UI Components and Pages that need to call this function:
```razor
   @inject IEmailService EmailService
   async Task SendEmail() => await EmailService.SendAsync(email);
```
3. In each platform project, create a class that inherits from this interface:
```csharp
   public class WebEmailService : IEmailService
   {
       public WebEmailService(IJSRuntime js) => Js = js;
       
       public IJSRuntime Js { get; }

       public async Task SendAsync(string email) => await Js.InvokeVoidAsync("goToHref", $"mailto:{email}");
   }
```
3. In the Program.cs/Startup.cs file for each platform project (eg. Web), set up Dependency Injection to inject the correct platform-specific class for the interface:
```csharp
public class Program 
{
   public static async Task Main(string[] args)
   {
       var builder = WebAssemblyHostBuilder.CreateDefault(args);
       ...
        builder.Services.AddScoped<IEmailService, WebEmailService>();
   }
}
```

#### Override UI Components and Pages

1. Create a .razor file in your Web Project with the same name, and under the same folder structure, as the .razor file in your UI Project that you wish to 
1. override. This component will automatically be used in place of the base UI component.