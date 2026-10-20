using Microsoft.AspNetCore.Http;
using Sparc.Blossom.Authentication;

namespace Sparc.Blossom.Platforms.Server;

public class BlossomAuthenticatorMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IBlossomAuthenticator auth)
    {
        if (context.IsStaticFileRequest())
        {
            await _next(context);
            return;
        }

        if (context.Request.Query.ContainsKey("_auth"))
        {
            // Handle TOTP requests separately
            var authCode = context.Request.Query["_auth"].ToString();
            var matchingUser = await auth.LoginAsync(authCode);
            if (matchingUser != null)
                context.Response.Redirect(context.Request.PathBase + context.Request.Path);
        }
        else if (context.User.Identity?.IsAuthenticated != true)
        {
            await auth.RegisterAsync();
        }

        await _next(context);
    }
}

