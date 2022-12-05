using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Sparc.Authentication;

public class LogoutModel : PageModel
{
    public async Task<IActionResult> OnGet()
    {
        // await HttpContext.SignOutAsync();
        return Page();
    }
}
