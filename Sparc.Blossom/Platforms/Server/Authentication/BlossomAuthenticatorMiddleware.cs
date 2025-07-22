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

        if (context.Request.Query.ContainsKey("_totp"))
        {
            // Handle TOTP requests separately
            var totpCode = context.Request.Query["_totp"].ToString();
            var matchingUser = await aura.Login($"totp:{totpCode}");
            if (matchingUser != null)
            {
                context.User = await auth.LoginAsync(matchingUser.ToUser().ToPrincipal());
                await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, context.User, new() { IsPersistent = true });
                context.Response.Redirect(context.Request.PathBase + context.Request.Path);
            }

            await _next(context);
            return;
        }

        var priorUser = BlossomUser.FromPrincipal(context.User);
        var user = await auth.GetAsync(context.User);

        if (user != null && (context.User.Identity?.IsAuthenticated != true || !priorUser.Equals(user)))
        {
            context.User = await auth.LoginAsync(context.User);
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, context.User, new() { IsPersistent = true });
        }

        await _next(context);
    }
}

