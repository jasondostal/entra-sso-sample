using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EntraSsoSample.Pages;

// No [Authorize] needed — the global FallbackPolicy in Program.cs already
// requires an authenticated user for every page.
public class IndexModel : PageModel
{
    public void OnGet()
    {
    }
}
