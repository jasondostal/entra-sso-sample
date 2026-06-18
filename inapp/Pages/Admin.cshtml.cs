using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EntraSsoSample.InApp.Pages;

// ROLE-BASED authorization: this page is reachable only by holders of the Admin app
// role. The policy ("RequireAdmin" → RequireRole("Admin")) is defined in Program.cs.
[Authorize(Policy = "RequireAdmin")]
public class AdminModel : PageModel
{
    public void OnGet() { }
}
