using System.Security.Claims;

namespace EntraSsoSample.Api.Authorization;

// Helpers that read the two DIFFERENT things an Entra access token can carry:
//   • delegated SCOPES  → "scp" claim  (a user signed in and delegated permission)
//   • app ROLES         → "roles" claim (a daemon/app authenticated as itself)
// Keeping these distinct is the whole point — see ScopeOrAppRoleHandler.
public static class TokenExtensions
{
    public static IReadOnlyCollection<string> Scopes(this ClaimsPrincipal user)
    {
        // v2 tokens use "scp"; v1 tokens use the long scope URI. Both are space-delimited.
        var scp = user.FindFirst("scp")?.Value
               ?? user.FindFirst("http://schemas.microsoft.com/identity/claims/scope")?.Value;
        return scp?.Split(' ', StringSplitOptions.RemoveEmptyEntries) ?? [];
    }

    public static IReadOnlyCollection<string> AppRoles(this ClaimsPrincipal user)
        => user.FindAll("roles").Select(c => c.Value).ToArray();

    // A token is "app-only" (client-credentials / daemon) when it carries app roles but
    // NO delegated scope. There is no signed-in user behind it — so owner/user checks
    // don't apply, and you must NOT treat a delegated scope as an app permission.
    public static bool IsAppOnly(this ClaimsPrincipal user)
        => user.Scopes().Count == 0 && user.AppRoles().Count > 0;
}
