using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Sparc.Blossom.Authentication;

namespace Sparc.Engine;

public class SparcEngineAuthenticatorMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IBlossomAuthenticator auth)
    {
        if (context.Request.Path.StartsWithSegments("/_blazor") || context.Request.Path.StartsWithSegments("/_framework"))
        {
            await _next(context);
            return;
        }

        var priorUser = BlossomUser.FromPrincipal(context.User);
        var user = await auth.GetAsync(context.User);

        if (user != null && (context.User.Identity?.IsAuthenticated != true || !priorUser.Equals(user)))
        {
            context.User = await auth.LoginAsync(context.User);
            await context.SignOutAsync();
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, context.User, new() { IsPersistent = true });
        }

        await _next(context);
    }
}

