using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Graph;
using Microsoft.Identity.Client;
using Microsoft.Identity.Web;

namespace EntraSsoSample.InApp.Pages;

// [AuthorizeForScopes] is the magic that makes incremental consent work: if calling
// Graph throws because we lack consent / a fresh token, this filter catches it and
// bounces the user back to Entra to consent, then returns here. Without it, the first
// Graph call after sign-in could just fail.
[AuthorizeForScopes(Scopes = ["User.Read"])]
public class IndexModel : PageModel
{
    private readonly GraphServiceClient _graph;

    public IndexModel(GraphServiceClient graph) => _graph = graph;

    // From Microsoft Graph (an ACCESS token was used to call /me).
    public string? GraphDisplayName { get; private set; }
    public string? GraphUpn { get; private set; }
    public string? GraphJobTitle { get; private set; }
    public string? GraphError { get; private set; }

    // From the ID token (who Entra says you are — no API call needed).
    public List<ClaimRow> Claims { get; } = new();

    public record ClaimRow(string Type, string Value);

    public async Task OnGetAsync()
    {
        // The ID-token claims the OIDC sign-in put on the user principal.
        foreach (var c in User.Claims)
        {
            Claims.Add(new ClaimRow(c.Type, c.Value));
        }

        // Now prove we can call a downstream API with an ACCESS token: GET /me.
        try
        {
            var me = await _graph.Me.GetAsync();
            GraphDisplayName = me?.DisplayName;
            GraphUpn = me?.UserPrincipalName;
            GraphJobTitle = me?.JobTitle;
        }
        catch (MsalUiRequiredException)
        {
            // Let it propagate: [AuthorizeForScopes] turns this into a consent redirect.
            throw;
        }
        catch (Exception ex)
        {
            // Any other Graph failure — surface it on the page instead of 500-ing.
            GraphError = ex.Message;
        }
    }
}
