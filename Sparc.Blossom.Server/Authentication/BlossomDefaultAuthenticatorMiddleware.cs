using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Http;

namespace Sparc.Blossom.Authentication;

public class BlossomDefaultAuthenticatorMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    // IMessageWriter is injected into InvokeAsync
    public async Task InvokeAsync(HttpContext context, IBlossomAuthenticator auth)
    {
        var user = await auth.GetAsync(context.User);
        if (user != null && context.User.Identity?.IsAuthenticated != true)
        {
            context.User = user!.CreatePrincipal();
            await context.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, context.User);
        }

        await _next(context);
    }
}

