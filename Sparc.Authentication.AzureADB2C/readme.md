# Sparc.Authentication.AzureADB2C

[![Nuget](https://img.shields.io/nuget/v/Sparc.Authentication.AzureADB2C?label=Sparc.Authentication.AzureADB2C)](https://www.nuget.org/packages/Sparc.Authentication.AzureADB2C/)

The `Sparc.Authentication.AzureADB2C` plugin is a plug-and-play authentication plugin that hooks up all API authentication in your Features Project to [Azure AD B2C](https://docs.microsoft.com/en-us/azure/active-directory-b2c/).

Add this plugin to your Features Project if you'd like to use Azure AD B2C as your app's authentication provider.

## Get Started with Sparc.Authentication.AzureADB2C

### Set up Azure Active Directory B2C

Follow the guide [here](https://docs.microsoft.com/en-us/aspnet/core/blazor/security/webassembly/hosted-with-azure-active-directory-b2c?view=aspnetcore-6.0) to set up two applications within Azure AD B2C -- one for your Features Project, and one for your UI Project. 

Take note of the following items as you are setting things up. You will need these later on in the process:

- The name of your B2C domain
- The Client ID (Guid) of your Azure AD B2C Features (Server) Application
- The Client ID (Guid) of your Azure AD B2C UI (Client) Application
- The Sign Up/Sign In and Reset Password Policy IDs
- The URI of the scope you create to connect the Features (Server) & UI (Client) applications

### In Your Features Project:

1. Add the `Sparc.Authentication.AzureADB2C` Nuget package:
[![Nuget](https://img.shields.io/nuget/v/Sparc.Authentication.AzureADB2C?label=Sparc.Authentication.AzureADB2C)](https://www.nuget.org/packages/Sparc.Authentication.AzureADB2C/)

2. Add the following settings to your `appsettings.json` file, replacing the values as necessary to match your application:
	```json
	{
      "AzureAdB2C": {
        "Instance": "https://mydomainname.b2clogin.com/tfp/",
        "ClientId": "00000000-0000-0000-0000-000000000000", // the Client ID of your Azure AD B2C Features/API Project
        "Domain": "mydomainname.onmicrosoft.com",
        "SignUpSignInPolicyId": "B2C_1_SignIn_SignUp",
        "ResetPasswordPolicyId": "B2C_1_ForgotPassword"
	  }
	}
	```

3. Add the following line of code to your `Program.cs` file to register the `Sparc.Authentication.AzureADB2C` plugin. It will automatically read the data from the `AzureAdB2C` configuration in your `appsettings.json`.
    ```csharp
    builder.Services.AddAzureADB2CAuthentication(builder.Configuration);
	```

### In your Platform projects (Web/Maui):

1. Add the following settings to your `wwwroot/appsettings.json` file, replacing the values as necessary to match your application:
    ```json
    {
      "AzureAdB2C": {
        "Authority": "https://mydomainname.b2clogin.com/tfp/mydomainname.onmicrosoft.com/B2C_1_SignIn_SignUp",
        "ClientId": "00000000-0000-0000-0000-000000000000", // the Client ID of your Azure AD B2C UI Project
        "ValidateAuthority": false
      }
    }
    ```

2. You just need to add Blossom to your platform project to register the client features in Authentication, add the following line of code to your `Program.cs` or `MauiProgram.cs` file.
 
    ```csharp
        builder.AddBlossom<MyAppApi>(builder.Configuration["ApiUrl"]);
    ```
> pass in your auto-generated Api class type (generated from your OpenApiReference -- more info in the [Sparc.Blossom documentation](/Sparc.Blossom))

3. *(Web projects only)* Add the following line of code to your `index.html` file. This adds the necessary JS library for Blazor Web Assembly Authentication using MSAL:
    ```html
    <script src="_content/Microsoft.Authentication.WebAssembly.Msal/AuthenticationService.js"></script>
    ```

4. Run your solution. All of your Features will be automatically protected with JWT-based access tokens, and these access tokens will be sent automatically when the users are logged in.

5. To log in, set your login button to navigate to `/authentication/login?returnUrl=`. To log out, navigate to `/authentication/logout`.
