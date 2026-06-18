using EntraSsoSample.Api.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace EntraSsoSample.Api.Authorization;

// Per-resource decision: allow if the caller OWNS the document, or is ELEVATED
// (holds Reports.ReadWrite as a delegated scope or an app role). The endpoint's
// coarse policy (Reports.Read) has already run; this refines it to the object.
public sealed class DocumentAuthorizationHandler
    : AuthorizationHandler<SameOwnerOrElevatedRequirement, Document>
{
    private const string Elevated = "Reports.ReadWrite";

    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameOwnerOrElevatedRequirement requirement,
        Document resource)
    {
        var oid = context.User.GetObjectId();   // null for app-only tokens (no user)
        var isOwner = oid is not null && oid == resource.OwnerObjectId;
        var isElevated = context.User.Scopes().Contains(Elevated)
                      || context.User.AppRoles().Contains(Elevated);

        if (isOwner || isElevated)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
