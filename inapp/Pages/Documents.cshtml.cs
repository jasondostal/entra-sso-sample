using EntraSsoSample.InApp.Authorization;
using EntraSsoSample.InApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Identity.Web;

namespace EntraSsoSample.InApp.Pages;

// RESOURCE-BASED authorization + how roles feed into it:
//   • Read an item  → owner, Auditor (read-all), or Admin
//   • Edit an item  → owner or Admin
//   • Create a draft → needs the Contributor role (a plain role check)
public class DocumentsModel : PageModel
{
    private readonly IAuthorizationService _authz;

    public DocumentsModel(IAuthorizationService authz) => _authz = authz;

    public List<Row> Rows { get; } = new();
    public string? Flash { get; private set; }

    public record Row(Document Doc, bool IsOwner, bool CanRead, bool CanEdit);

    // One doc owned by the current user, two owned by other people.
    private List<Document> Seed()
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
        var doc = Seed().FirstOrDefault(d => d.Id == id);
        if (doc is null) return NotFound();

        // The real enforcement — re-checked server-side on every mutation.
        var result = await _authz.AuthorizeAsync(User, doc, DocumentOperations.Edit);
        Flash = result.Succeeded
            ? $"✅ Authorized — you edited \"{doc.Title}\"."
            : $"❌ Denied — you can't edit \"{doc.Title}\" (you're not the owner or an Admin).";

        await BuildRowsAsync();
        return Page();
    }

    public async Task<IActionResult> OnPostCreateAsync()
    {
        // A plain role check (no resource): creating needs the Contributor role.
        var result = await _authz.AuthorizeAsync(User, "RequireContributor");
        Flash = result.Succeeded
            ? "✅ Draft created (you hold the Contributor role)."
            : "❌ Denied — creating a draft needs the Contributor role.";

        await BuildRowsAsync();
        return Page();
    }

    private async Task BuildRowsAsync()
    {
        Rows.Clear();
        var myOid = User.GetObjectId();
        foreach (var doc in Seed())
        {
            var canRead = (await _authz.AuthorizeAsync(User, doc, DocumentOperations.Read)).Succeeded;
            var canEdit = (await _authz.AuthorizeAsync(User, doc, DocumentOperations.Edit)).Succeeded;
            Rows.Add(new Row(doc, myOid == doc.OwnerObjectId, canRead, canEdit));
        }
    }
}
