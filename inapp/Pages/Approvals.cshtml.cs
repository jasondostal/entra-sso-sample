using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace EntraSsoSample.InApp.Pages;

// ROLE-BASED: reachable only by Approver (or Admin). A user without the role is sent
// to /AccessDenied before this page runs — enforced by the policy, no in-page checks.
[Authorize(Policy = "RequireApprover")]
public class ApprovalsModel : PageModel
{
    public string? Flash { get; private set; }

    public string[] Pending { get; } = ["Expense #4471", "Vendor onboarding: Contoso", "Wire release #88120"];

    public void OnGet() { }

    public IActionResult OnPostApprove(string item)
    {
        Flash = $"✅ Approved \"{item}\".";
        return Page();
    }
}
