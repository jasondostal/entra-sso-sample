using EntraSsoSample.InApp.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authorization.Infrastructure;
using Microsoft.Identity.Web;

namespace EntraSsoSample.InApp.Authorization;

// Resource-based authorization, decided per-document AND per-operation:
//   Read → the owner, an Auditor (read-all), or an Admin
//   Edit → the owner or an Admin
// This is the layer roles/scopes can't reach — it sees the actual object.
public sealed class DocumentAuthorizationHandler
    : AuthorizationHandler<OperationAuthorizationRequirement, Document>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        OperationAuthorizationRequirement requirement,
        Document resource)
    {
        var callerOid = context.User.GetObjectId();
        var isOwner = callerOid is not null && callerOid == resource.OwnerObjectId;
        var isAdmin = context.User.IsInRole(Roles.Admin);
        var isAuditor = context.User.IsInRole(Roles.Auditor);

        var allowed = requirement.Name switch
        {
            nameof(DocumentOperations.Read) => isOwner || isAuditor || isAdmin,
            nameof(DocumentOperations.Edit) => isOwner || isAdmin,
            _ => false,
        };

        if (allowed)
        {
            context.Succeed(requirement);
        }
        return Task.CompletedTask;
    }
}
