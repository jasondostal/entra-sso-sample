using EntraSsoSample.InApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;

namespace EntraSsoSample.InApp.Pages;

// Demonstrates RESOURCE-BASED authorization: the same user is allowed to edit some
// documents and not others, decided per-row against the actual Document.
public class DocumentsModel : PageModel
{
    private readonly IAuthorizationService _authz;

    public DocumentsModel(IAuthorizationService authz) => _authz = authz;

    public List<Row> Rows { get; } = new();
    public string? Flash { get; private set; }

    // CanEdit is the real authorization decision; IsOwner / IsAdmin are shown only to
    // make the "why" visible on the page.
    public record Row(Document Doc, bool IsOwner, bool IsAdmin, bool CanEdit);

    // In a real app these come from a store. We seed one doc owned by the current user
    // (so owner-match is demonstrable) and two owned by other people.
    private List<Document> SeedDocuments()
    {
        var myOid = User.GetObjectId() ?? "you";
        var myName = User.Identity?.Name ?? "you";
        return new()
        {
            new Document(1, "My quarterly report", myOid, myName),
            new Document(2, "Alice's budget draft", "11111111-1111-1111-1111-111111111111", "Alice Example"),
            new Document(3, "Bob's policy memo",   "22222222-2222-2222-2222-222222222222", "Bob Example"),
        };
    }

    public async Task OnGetAsync() => await BuildRowsAsync();

    public async Task<IActionResult> OnPostEditAsync(int id)
    {
        var doc = SeedDocuments().FirstOrDefault(d => d.Id == id);
        if (doc is null) return NotFound();

        // The REAL enforcement. Never trust the button's enabled/disabled state — the
        // server re-checks the resource-based policy on every mutating request.
        var result = await _authz.AuthorizeAsync(User, doc, "EditDocument");
        if (!result.Succeeded)
        {
            // Production APIs return Forbid() (403). We render a message so the demo
            // stays on one page instead of redirecting to an access-denied screen.
            Flash = $"❌ Denied — you may not edit \"{doc.Title}\".";
        }
        else
        {
            Flash = $"✅ Authorized — you edited \"{doc.Title}\".";
        }

        await BuildRowsAsync();
        return Page();
    }

    private async Task BuildRowsAsync()
    {
        Rows.Clear();
        var isAdmin = User.IsInRole("Admin");
        var myOid = User.GetObjectId();
        foreach (var doc in SeedDocuments())
        {
            var decision = await _authz.AuthorizeAsync(User, doc, "EditDocument");
            Rows.Add(new Row(doc, myOid == doc.OwnerObjectId, isAdmin, decision.Succeeded));
        }
    }
}
