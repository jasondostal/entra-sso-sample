using Microsoft.AspNetCore.Authorization;

namespace EntraSsoSample.Api.Authorization;

public sealed class ScopeOrAppRoleHandler : AuthorizationHandler<ScopeOrAppRoleRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        ScopeOrAppRoleRequirement requirement)
    {
        var user = context.User;
        var scopes = user.Scopes();

        if (scopes.Count > 0)
        {
            // Delegated token → authorize on SCOPES only. Deliberately do not fall back
            // to app roles: a user token shouldn't be granted via an app permission.
            if (requirement.Scopes.Intersect(scopes).Any())
            {
                context.Succeed(requirement);
            }
            return Task.CompletedTask;
        }

        // App-only token → authorize on APP ROLES only.
        if (requirement.AppRoles.Intersect(user.AppRoles()).Any())
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
