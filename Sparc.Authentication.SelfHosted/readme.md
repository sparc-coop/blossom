# Sparc.Authentication.SelfHosted

[![Nuget](https://img.shields.io/nuget/v/Sparc.Authentication.SelfHosted?label=Sparc.Authentication.SelfHosted)](https://www.nuget.org/packages/Sparc.Authentication.SelfHosted/)

The `Sparc.Authentication.SelfHosted` plugin is a plug-and-play authentication plugin that uses a custom OAuth2.0 API authentication protocol built into Sparc.Kernel.

Add this plugin to your Features Project if you'd like to use a custom authentication platform with greater control. You will need to handle all user persistence in this scenario by configuring a `SparcAuthenticationProvider` (details below).

## Get Started with Sparc.Authentication.SelfHosted

### In Your Features Project:

1. Add the `Sparc.Authentication.SelfHosted` Nuget package:
[![Nuget](https://img.shields.io/nuget/v/Sparc.Authentication.SelfHosted?label=Sparc.Authentication.SelfHosted)](https://www.nuget.org/packages/Sparc.Authentication.SelfHosted/)

2. Add the following settings to your `appsettings.json` file, replacing the values as necessary to match your application:
	```json
	{
      "BaseUrl": "https://localhost:7147", // The URL of your Features Project
      "WebClientUrl": "https://localhost:7001", // The URL of your Web Project
      "MobileClientUrl": "myapp://" // The registered URI scheme of your MAUI Project
	}
	```

3. Create a class that inherits from `SparcAuthenticator` and minimally provides an implementation for the following abstract methods in the class:

    - `async Task<List<Claim>> GetClaimsAsync(string userId);`
    - `async Task<bool> IsActiveAsync(string userId);`
    - `async Task<bool> LoginAsync(string userName, string password);`
    - `string? GetUserId(ClaimsPrincipal? principal);`

4. Add the following line of code to your `Startup.cs` file to register the `Sparc.Authentication.SelfHosted` plugin. Pass in:

    - The class type of your derived SparcAuthenticator class.
    - The base URL of your Features Project. This registers the OAuth Authority. 
    - The name and URL of your Web Project. This sets the Web Project up as a valid client with access to your Features Project, and configures CORS.
    - The name and URI scheme of your MAUI Project. This sets the MAUI Project up as a valid client with access to your Features Project.

    ```csharp
    services.AddSelfHostedAuthentication<MyAppAuthenticator>(Configuration["BaseUrl"],
    ("Web", Configuration["WebClientUrl"]),
    ("Mobile", Configuration["MobileClientUrl"]));

	```


### In your Platform projects (Web/Maui):

1. Add the following line of code to your `Startup.cs` or `MauiProgram.cs` file to register the client authentication features. Pass in:
 
    - your auto-generated Api class type (generated from your OpenApiReference -- more info in the [Sparc.UI documentation](/Sparc.UI))
    - the name of your Features Api (this will be used as the OAuth2 allowed scope)
    - the base URL of your Features Project (this sets up the base URL for your auto-generated Api class and configures it for proper authentication headers)
    - the client ID that matches the client ID you set up in your Features Project for this client project (in this case, "Web" or "Mobile")

    ```csharp
       builder.AddSelfHostedApi<MyContactNetworkApi>(
            "MyApp API",
            builder.Configuration["ApiUrl"],
            "Mobile");
    ```

    or

    ```csharp
       builder.Services.AddSelfHostedApi<MyContactNetworkApi>(
            "MyApp API",
            builder.Configuration["ApiUrl"],
            "Web");
    ```
    > Note: This registration method exists within the Sparc.Platforms.* projects. There is no need to add this plugin directly to your Platform Projects.

4. Run your solution. All of your Features will be automatically protected with JWT-based access tokens, and these access tokens will be sent automatically when
the users are logged in.

5. To log in, set your login button to navigate to `/authentication/login?returnUrl=`. To log out, navigate to `/authentication/logout`.