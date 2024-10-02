using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;

namespace Sparc.Blossom.Authentication;

public class BlossomDefaultAuthenticatorMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IBlossomAuthenticator auth)
    {
        var user = await auth.GetAsync(context.User);
        if (user != null && context.User.Identity?.IsAuthenticated != true)
        {
            context.User = user!.CreatePrincipal();
            var authProperties = new AuthenticationProperties
            {
                IsPersistent = true
            };
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, context.User, authProperties);
        }

        await _next(context);
    }
}

