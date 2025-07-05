namespace Sparc.Engine;

public class SparcCookieHandler(IHttpContextAccessor httpContextAccessor) : DelegatingHandler
{
    private readonly IHttpContextAccessor _httpContextAccessor = httpContextAccessor;
    private const string CookieName = ".Sparc.Cookie";

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // Forward the .Sparc.Cookie from the incoming request if present
        var context = _httpContextAccessor.HttpContext;
        var cookie = context?.Request.Cookies[CookieName];
        if (!string.IsNullOrEmpty(cookie))
        {
            request.Headers.Add("Cookie", $"{CookieName}={cookie}");
        }

        var response = await base.SendAsync(request, cancellationToken);

        // Capture Set-Cookie from Login response and set it in the browser
        if (request.RequestUri?.AbsolutePath.Contains("/auth/login") == true && response.Headers.TryGetValues("Set-Cookie", out var setCookies))
        {
            foreach (var setCookie in setCookies.Where(x => x.StartsWith(CookieName)))
            {
                // Set the cookie in the response
                var cookieValue = setCookie.Split(';')[0].Split('=')[1];
                context?.Response.Cookies.Append(CookieName, cookieValue, new CookieOptions
                {
                    HttpOnly = true,
                    Secure = true,
                    SameSite = SameSiteMode.Lax
                });
            }
        }

        return response;
    }
}
