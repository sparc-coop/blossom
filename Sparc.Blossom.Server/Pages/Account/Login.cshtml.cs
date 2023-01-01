using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

[BindProperties]
[Authorize(AuthenticationSchemes = CookieAuthenticationDefaults.AuthenticationScheme)]
[AllowAnonymous]
public class LoginModel : PageModel
{
    public LoginModel(BlossomAuthenticator authenticator)
    {
        Authenticator = authenticator;
    }

    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Error { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    private BlossomAuthenticator Authenticator { get; }

    public async Task<IActionResult> OnGet()
    {
        if (User.Identity?.IsAuthenticated == true && ReturnUrl != null)
        {
            var user = await Authenticator.LoginAsync(User);
            if (user != null)
                return await LoginAsync(user.CreatePrincipal());
        }
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ReturnUrl == null) return Page();
        return await PerformLogin();
    }

    async Task<IActionResult> PerformLogin()
    {
        Error = null;
        if (string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Email)) 
            return Page();

        var user = await Authenticator.LoginAsync(Email, Password);

        if (user != null)
            return await LoginAsync(user.CreatePrincipal());

        Error = "Could not log you in! Please try entering your code again or request a new code.";
        return Page();

    }

    private async Task<IActionResult> LoginAsync(ClaimsPrincipal principal)
    {
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);

        var returnUri = new Uri(ReturnUrl!);
        var token = Authenticator.CreateToken(principal);
        var callbackUrl = $"{returnUri.Scheme}://{returnUri.Authority}/_authorize";
        callbackUrl = QueryHelpers.AddQueryString(callbackUrl, "returnUrl", ReturnUrl!);
        callbackUrl = QueryHelpers.AddQueryString(callbackUrl, "token", token);
        return Redirect(callbackUrl);
    }
}
