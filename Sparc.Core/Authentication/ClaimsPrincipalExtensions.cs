using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

public static class ClaimsPrincipalExtensions
{
    public static string Id(this ClaimsPrincipal principal) => 
           principal.FindFirst(x => x.Type == ClaimTypes.NameIdentifier)?.Value
        ?? principal.FindFirst(x => x.Type == "sub")?.Value
        ?? string.Empty;

    public static bool IsAnonymous(this ClaimsPrincipal principal) => 
        principal.Identity?.AuthenticationType == "Anonymous" || Guid.TryParse(principal.Identity?.Name, out _);

    public static string? Email(this ClaimsPrincipal principal) =>
        principal.Get(ClaimTypes.Email)
        ?? principal.Get("emails");

    public static string? FirstName(this ClaimsPrincipal principal) =>
        principal.Get("given_name")
        ?? principal.Get("http://schemas.microsoft.com/identity/claims/givenname")
        ?? principal.Get(ClaimTypes.GivenName);

    public static string? LastName(this ClaimsPrincipal principal) =>
        principal.Get("family_name")
        ?? principal.Get("http://schemas.microsoft.com/identity/claims/lastname")
        ?? principal.Get("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/surname")
        ?? principal.Get("surname")
        ?? principal.Get(ClaimTypes.Name);

    public static string? Get(this ClaimsPrincipal principal, string key) => principal?.FindFirst(key)?.Value;
}
