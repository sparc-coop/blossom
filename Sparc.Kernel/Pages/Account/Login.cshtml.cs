using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.WebUtilities;

namespace Sparc.Authentication;

[BindProperties]
public class LoginModel : PageModel
{
    public LoginModel(SparcAuthenticator authenticator)
    {
        Authenticator = authenticator;
    }

    public string? Email { get; set; }
    public string? Password { get; set; }
    public string? Error { get; set; }
    [BindProperty(SupportsGet = true)]
    public string? ReturnUrl { get; set; }

    private SparcAuthenticator Authenticator { get; }

    public IActionResult OnGet()
    {
        if (User.Identity?.IsAuthenticated == true && ReturnUrl != null)
            return Redirect(ReturnUrl);
        
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
        {
            await HttpContext.SignInAsync(user.CreatePrincipal());
            
            var returnUri = new Uri(ReturnUrl!);
            var token = Authenticator.CreateToken(user);
            var callbackUrl = $"{returnUri.Scheme}://{returnUri.Authority}/_authorize";
            callbackUrl = QueryHelpers.AddQueryString(callbackUrl, "returnUrl", ReturnUrl!);
            callbackUrl = QueryHelpers.AddQueryString(callbackUrl, "token", token);
            return Redirect(callbackUrl);
        }
        
        Error = "Could not log you in! Please try entering your code again or request a new code.";
        return Page();

    }
}
