using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Security.Claims;

namespace Sparc.Blossom.Authentication;

[BindProperties]
[Authorize]
[AllowAnonymous]
public class LoginModel(BlossomAuthenticator authenticator) : PageModel
{
    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Error { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    private BlossomAuthenticator Authenticator { get; } = authenticator;

    public async Task<IActionResult> OnGet()
    {
        if (User.Identity?.IsAuthenticated == true && ReturnUrl != null)
        {
            var user = await Authenticator.GetAsync();
            if (user != null)
                return await LoginAsync(user.CreatePrincipal());
        }
        
        return Page();
    }

    public async Task<IActionResult> OnPostAsync()
    {
        if (ReturnUrl == null) return Page();
        return await LoginAsync();
    }

    public virtual async Task<IActionResult> LoginAsync()
    {
        Error = null;
        if (string.IsNullOrWhiteSpace(Password) || string.IsNullOrWhiteSpace(Email)) 
            return Page();

        var user = await Authenticator.LoginAsync(Email);

        if (user != null)
            return await LoginAsync(user.CreatePrincipal());

        Error = "Could not log you in! Please try entering your code again or request a new code.";
        return Page();
    }

    protected async Task<IActionResult> LoginAsync(ClaimsPrincipal principal)
    {
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal);
        return Redirect(ReturnUrl!);
    }
}
