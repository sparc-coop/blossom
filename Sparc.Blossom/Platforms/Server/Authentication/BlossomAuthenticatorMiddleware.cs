using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;
using Sparc.Blossom.Authentication;
using Sparc.Engine.Aura;

namespace Sparc.Blossom.Platforms.Server;

public class BlossomAuthenticatorMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IBlossomAuthenticator auth, ISparcAura aura)
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
            var matchingUser = await aura.Login(authCode);
            if (matchingUser != null)
            {
                await auth.LoginAsync(matchingUser.ToUser().ToPrincipal());
                context.Response.Redirect(context.Request.PathBase + context.Request.Path);
            }

            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated != true)
            await auth.RegisterAsync();

        await _next(context);
    }
}

