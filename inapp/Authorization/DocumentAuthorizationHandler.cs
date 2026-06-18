using EntraSsoSample.InApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

namespace EntraSsoSample.InApp.Authorization;

// Evaluates SameOwnerOrAdminRequirement for a specific Document: allow if the caller
// OWNS the document, OR if they hold the Admin app role. The strongly-typed resource
// (Document) is the whole point of AuthorizationHandler<TRequirement, TResource>.
public sealed class DocumentAuthorizationHandler
    : AuthorizationHandler<SameOwnerOrAdminRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SameOwnerOrAdminRequirement requirement,
        Document resource)
    {
        var callerOid = context.User.GetObjectId();   // the caller's Entra object id (oid)
        var isOwner = callerOid is not null && callerOid == resource.OwnerObjectId;
        var isAdmin = context.User.IsInRole("Admin");

        if (isOwner || isAdmin)
        {
            context.Succeed(requirement);
        }
        // Don't call context.Fail(): another handler might still grant it. Simply not
        // succeeding is a denial unless something else succeeds.
        return Task.CompletedTask;
    }
}
