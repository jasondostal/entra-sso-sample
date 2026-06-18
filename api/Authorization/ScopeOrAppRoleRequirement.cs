using Microsoft.AspNetCore.Authorization;

namespace EntraSsoSample.Api.Authorization;

// Authorize a call if EITHER:
//   • it's a delegated (user) token holding one of `Scopes`, OR
//   • it's an app-only (daemon) token holding one of `AppRoles`.
// This is how one endpoint can serve both a user-facing front end and a back-end
// service, without conflating the two permission models.
public sealed class ScopeOrAppRoleRequirement(string[] scopes, string[] appRoles) : IAuthorizationRequirement
{
    public string[] Scopes { get; } = scopes;
    public string[] AppRoles { get; } = appRoles;
}
