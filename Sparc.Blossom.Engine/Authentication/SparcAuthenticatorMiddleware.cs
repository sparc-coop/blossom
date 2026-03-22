namespace Sparc.Blossom.Authentication;

public class SparcAuthenticatorMiddleware(RequestDelegate next)
{
    private readonly RequestDelegate _next = next;

    public async Task InvokeAsync(HttpContext context, IBlossomAuthenticator auth)
    {
        if (context.IsStaticFileRequest() || context.Request.Headers.ContainsKey("Stripe-Signature"))
        {
            await _next(context);
            return;
        }

        if (context.User.Identity?.IsAuthenticated == true)
        {   // If the user is already authenticated, we can skip the authentication process.
            await _next(context);
            return;
        }

        var bearerToken = context.Request.Headers.Authorization.FirstOrDefault()?.Split(" ").Last();
        if (bearerToken != null && bearerToken.Length == 32)
        {
            // Look up domain by bearer token and set the user principal if found
            await auth.LoginAsync(context.User, "Bearer", bearerToken);
            await _next(context);
            return;
        }

        await auth.LoginAsync(context.User);
        await _next(context);
    }
}

