# Sparc.Blossom

[![Nuget](https://img.shields.io/nuget/v/Sparc.Blossom?label=Sparc.Blossom)](https://www.nuget.org/packages/Sparc.Blossom/)

The `Sparc.Blossom` library is the main framework library for the *Platform Project* in your Blossom solution, examples of Platforms Projects are Web and MAUI.

# Table of contents
- [Get Started with a Web Project](https://github.com/sparc-coop/blossom/tree/main/Sparc.Blossom#get-started-with-a-web-project)
	- [Run and Debug a Web Project Locally](https://github.com/sparc-coop/blossom/tree/main/Sparc.Blossom#run-and-debug-a-web-project-locally)
	- [Deploy your Web Project to the Web](https://github.com/sparc-coop/blossom/tree/main/Sparc.Blossom#deploy-your-web-project-to-the-web)
- [What is a MAUI Project?](https://github.com/sparc-coop/blossom/tree/main/Sparc.Blossom#what-is-a-maui-project)
	- Get Started with a MAUI Project
	- Run and Debug a MAUI Project Locally
	- Deploy your MAUI Project
- Shared UI
- Examples/Templates
	- Pages and components
- [FAQ](https://github.com/sparc-coop/blossom/tree/main/Sparc.Blossom#faq)
	- How do I create MAUI platform-specific code in multi-platform projects?

## Get Started with a Web Project
A *Web Project* is the project that you intend to deploy to Web platforms for use in a web browser.

If your project is multi-platform (i.e. web + desktop and/or mobile), this project should ideally contain only **startup** and **platform-specific** code.

If your project is web-only, the *Web Project* can also contain all of the UI components and pages for your project. 
Otherwise, the UI components and pages should go into a shared [Sparc.UI](/Sparc.UI) project, so 
that the mobile/desktop (MAUI) project can use the same UI components.

A *Web Project* is currently driven by Blazor Web Assembly. This way it acts as a self-contained application similar to mobile and desktop.

Here are the steps to create your Web Project:

1. Create a New Blazor Web Assembly Empty project (preferably called [YourProject].Web
2. Add the `Sparc.Blossom` Nuget package to your project
3. Make sure it's running on the same address you defined as `WebClientUrl` in your *Features* project, look at the `applicationUrl` property inside the `Properties` > `launchSettings.json`
4. Add a `appsettings.json` inside the `wwwroot` folder and add the following setting (this must match the *Features* project Url
```json
{ "ApiUrl": "https://localhost:7001" }
```
4. Add the following line to your `Program.cs`
5. Modify the `App.razor` file at the root of your Web Project, fully replacing its contents with the following:
```html
<Sparc.Blossom.Platforms.Web.BlossomApp MainLayout="typeof(MainLayout)"
                                            Startup="typeof(Program)" />
```
6. Connect to your *Features* project using 
7. Write your app. 
	a. (Web-only projects) Write your UI pages and components directly in the Web Project, using guidance from the examples. Also make sure to follow the "Connect Your UI to your Features" instructions, replacing the UI Project with your Web Project. 
	b. (Multi-platform projects) Create a Sparc.UI project, reference it from your Web Project, and write your UI components within the UI project.

### Run and Debug a Web Project Locally

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

### Deploy your Web Project to the Web

Your *Web Project* is directly deployable to any Web Assembly-compatible host (such as Azure App Service). Simply use the normal publishing procedures
to publish your project like any other Web project.

Once deployed, ensure that the `ApiUrl` and `WebClientUrl` settings are updated to match the live URLs.

## What is a MAUI Project?

A *Maui Project* is the project that you intend to deploy to Mobile (Android/iOS) and Desktop (Windows/Mac) platforms.

If your project is multi-platform (i.e. desktop and/or mobile + web), this project should ideally contain only **startup** and **platform-specific** code.

If your project is mobile/desktop-only, the *MAUI Project* can also contain all of the UI components and pages for your project. 
Otherwise, the UI components and pages should go into a shared [Sparc.UI](/Sparc.UI) project, so 
that the Web Project can use the same UI components.

A *MAUI Project* is driven by [.NET MAUI](https://dotnet.microsoft.com/en-us/apps/maui), Microsoft's newest cross-platform development framework. .NET MAUI is currently in Release Candidate status, and 
Sparc.Kernel supports .NET MAUI Release Candidate.

However, we believe that true cross-platform development must also include Web, and .NET MAUI XAML files cannot yet be deployed directly to Web. 
This is why Sparc.Kernel works purely with **Blazor Pages and Components**, so that a true single Shared UI library can be constructed for Mobile, Desktop, and 
Web. 

.NET MAUI includes support for Blazor via an integrated WebView, and this is the functionality that Sparc.Kernel leverages.

## Get Started with a MAUI Project

1. Create a new *.NET MAUI Blazor App* project in your solution, preferably called *[Your Project]*.Maui.
2. Add the `Sparc.Blossom` Nuget package to your newly created project: 
[![Nuget](https://img.shields.io/nuget/v/Sparc.Blossom?label=Sparc.Blossom)](https://www.nuget.org/packages/Sparc.Blossom/)
3. Add the following line of code to your `MauiProgram.cs` file, in the appropriate method:

	```csharp
		public static class MauiProgram
		{
			public static MauiApp CreateMauiApp()
			{
				// Add/modify this line of code
				var builder = MauiApp.CreateBuilder().Sparcify<MainLayout>();

				...            
			}
	```
4. *(optional)* Add an `_Imports.razor` file to the root of your project, and add a `Sparc.Blossom` using statement to it:
	```razor
	@using Sparc.Blossom
	```

5. Write your app.
    a. *(Mobile/desktop-only projects)* Write your UI pages and components directly in the MAUI Project, using guidance from the [Sparc.UI documentation](/Sparc.UI).
    b. *(Multi-platform projects)* Create a [Sparc.UI](/Sparc.UI) project, reference it from your MAUI Project, and write your UI components within the UI project.

## Run and Debug a MAUI Project Locally

Since all UI and Features code is shared, it is normally easier to develop your application using the Web Project, except to test platform-specific code.

If you wish to run the MAUI Project locally, it should ideally be run in parallel with your Features Project, so that the local API can be accessed directly. 

Our favorite way to set this up with minimal issues is the following:

1. Right-click your MAUI Project -> Set as Startup Project.
2. Ensure your MAUI Startup settings are set correctly (i.e. Android Emulator).
3. Right-click your Features Project -> Set as Startup Project.
4. Ensure your Features Startup settings are set correctly (i.e. IIS Express or the Project Name itself), and take note of the assigned ports in `launchsettings.json` for the selected startup path.
5. Ensure that the `ApiUrl` in your `MauiProgram.cs` file points to the correct local port.
6. Right-click your Solution -> Set Startup Projects.
7. Set your Features and MAUI projects to "Start" to enable local debugging and Hot Reload.
8. Set all other projects to "None".
9. Run your solution. The projects will each run according to the settings you chose in steps #2 and #4, and full debugging + Hot Reload will be enabled for both projects.

## Deploy your MAUI Project

Your *MAUI Project* is directly deployable to the Google, Apple, and Windows stores, or as desired.

### All platforms:

1. Ensure that all API Url settings are updated to match the live URLs
2. Ensure that the `<ApplicationTitle>` and `<ApplicationVersion>` settings in your .csproj file are updated to correct values:
    ```xml
    <ApplicationTitle>My Wonderful Project</ApplicationTitle>
    <ApplicationVersion>205</ApplicationVersion>
    ```
3. Ensure that your Icon, Splash Screen, Images, and Fonts are all set up appropriately:
    ```xml
    <ItemGroup>
	    <MauiIcon Include="Resources\appicon.svg" ForegroundFile="Resources\appiconfg.svg" Color="#0d1637" />
	    <MauiSplashScreen Include="Resources\splash.svg" Color="#0d1637" />
	    <MauiImage Include="Resources\Images\*" />
	    <MauiFont Include="Resources\Fonts\*" />
	</ItemGroup>
    ```
### Android:

1. Add the following settings to your .csproj file, ensuring that the `<ApplicationId>` and `<SupportedOSPlatformVersion>` are set correctly:
    ```xml
	<PropertyGroup Condition="$(TargetFramework.Contains('android'))">
        <ApplicationId>com.myapplication.myapp</ApplicationId>
		<AndroidBuildApplicationPackage>true</AndroidBuildApplicationPackage>
		<AndroidLinkMode>None</AndroidLinkMode>
	    <SupportedOSPlatformVersion>21.0</SupportedOSPlatformVersion>
    </PropertyGroup>

	<PropertyGroup Condition="'$(Configuration)|$(TargetFramework)|$(Platform)'=='Release|net6.0-android|AnyCPU'">
		<AndroidPackageFormat>aab</AndroidPackageFormat>
		<DebugSymbols>false</DebugSymbols>
		<DebugType>pdbonly</DebugType>
		<Optimize>true</Optimize>
		<ErrorReport>prompt</ErrorReport>
		<WarningLevel>4</WarningLevel>
		<AndroidManagedSymbols>true</AndroidManagedSymbols>
		<RuntimeIdentifiers>android-arm;android-arm64;android-x86;android-x64</RuntimeIdentifiers>
		<AndroidUseSharedRuntime>false</AndroidUseSharedRuntime>
		<EmbedAssembliesIntoApk>true</EmbedAssembliesIntoApk>
		<AndroidCreatePackagePerAbi>false</AndroidCreatePackagePerAbi>
		<PlatformTarget>x64</PlatformTarget>
	</PropertyGroup>

    ```
2. To automatically sign the Android bundle file on build, use the following settings:
    ```xml
	<PropertyGroup Condition="$(TargetFramework.Contains('android'))">
		<AndroidKeyStore Condition="'$(Configuration)' == 'Release'">True</AndroidKeyStore>
		<AndroidSigningKeyAlias>[your keystore alias]</AndroidSigningKeyAlias>
		<AndroidSigningKeyPass>[your keystore password]</AndroidSigningKeyPass>
		<AndroidSigningKeyStore>[your keystore file name]</AndroidSigningKeyStore>
		<AndroidSigningStorePass>[your keystore password]</AndroidSigningStorePass>
    </PropertyGroup>
    ```

3. Build your MAUI Project in Release mode, then grab the signed .AAB file from `/bin/Release/net6.0-android` and upload it to the Play Store.

### iOS

1. Add the following settings to your .csproj file, ensuring that the `<ApplicationId>` and `<SupportedOSPlatformVersion>` are set correctly:
    ```xml
    <PropertyGroup Condition="$(TargetFramework.Contains('ios'))">
		<ApplicationId>com.myapplication.myapp</ApplicationId>
		<SupportedOSPlatformVersion>10.3.4</SupportedOSPlatformVersion>
		<AppleShortVersion>1.0</AppleShortVersion>
		<BuildIpa>true</BuildIpa>
		<TrimMode>Link</TrimMode>
		<MtouchLink>SdkOnly</MtouchLink>
		<PublishTrimmed>true</PublishTrimmed>
		<RuntimeIdentifier Condition="'$(Configuration)' == 'Debug'">iossimulator-x64</RuntimeIdentifier>
		<RuntimeIdentifier Condition="'$(Configuration)' == 'Release'">ios-arm64</RuntimeIdentifier>
	</PropertyGroup>
    ```

2. To automatically sign the IPA file on build, use the following settings:
	```xml
	<PropertyGroup Condition="$(TargetFramework.Contains('ios'))">
		<!-- Replace this value with the name of your Apple Signing certificate in your Keychain --> 
		<CodesignKey>Apple Distribution: MyApp Inc. (555X1284378)</CodesignKey>
		<!-- If you have any entitlements, make sure to add this line to embed them into the IPA -->
		<CodesignEntitlements>Entitlements.plist</CodesignEntitlements>
	</PropertyGroup>

3. Build your MAUI Project in Release mode. This will create a `.app` file.
4. To convert the `.app` file to an `.ipa` file for distribution, run the following script:
    ```bash
	mkdir bin/Release/net6.0-ios/ios-arm64/Payload
	cp -R bin/Release/net6.0-ios/ios-arm64/*.app bin/Release/net6.0-ios/ios-arm64/Payload
	cd bin/Release/net6.0-ios/ios-arm64
	/usr/bin/zip -r MyApp.ipa Payload
	/bin/rm -rf Payload
	```
5. Grab the signed `.ipa` file from `/bin/Release/net6.0-android` and upload it to the Play Store.

### Windows Desktop 

*Coming soon*

### Mac Desktop

*Coming soon*

## Examples / Templates
- Web Project only
	- [Ibis.Web](https://github.com/sparc-coop/ibis/tree/main/Ibis.Web)
	- [Kodekit.Web](https://github.com/sparc-coop/kodekit/tree/master/Kodekit.Web)
- Pages and components
	- [NewMessage.razor](https://github.com/sparc-coop/ibis/blob/main/Ibis.Web/Messages/NewMessage.razor)
	- [Installation.razor](https://github.com/sparc-coop/kodekit/blob/master/Kodekit.Web/Pages/Installation.razor)
## FAQ

### How do I create MAUI platform-specific code in multi-platform projects?

A MAUI Project can override any behavior from your UI project in two ways:

#### Override Behavior in Classes (best for logic and C# code)

1. Create an interface in your *UI Project* that contains the methods that need to be overridden (eg. `IEmailService`).
2. Use this interface in all UI Components and Pages that need to call this function:
	```razor
	   @inject IEmailService EmailService
	   async Task SendEmail() => await EmailService.SendAsync(email);
	```
3. In your MAUI Project, create a class that inherits from this interface:
	```csharp
	   public class MobileEmailService : IEmailService
	   {
		   public async Task SendAsync(string email) => await Email.ComposeAsync(new EmailMessage { To = new List<string> { email } });
	   }
	```
3. In the `MauiProgram.cs` file, set up Dependency Injection to inject the correct platform-specific class for the interface:
	```csharp
	public static class MauiProgram 
	{
	   public static MauiApp CreateMauiApp()
	   {
		   var builder = MauiApp.CreateBuilder().Sparcify<MainLayout>();
		   builder.Services.AddScoped<IEmailService, MobileEmailService>();
	   }
	}
	```

#### Override UI Components and Pages

1. Create a .razor file in your MAUI Project with the same name, and under the same folder structure, as the .razor file in your UI Project that you wish to 
override. This component will automatically be used in place of the base UI component.
