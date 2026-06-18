using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EntraSsoSample.InApp.Pages;

// Where the cookie handler sends a signed-in user who lacks a required role (403).
public class AccessDeniedModel : PageModel
{
    public void OnGet() { }
}
