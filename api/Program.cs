// A protected ASP.NET Core Web API secured by Microsoft Entra — the reference shape
// for "APIs coming fast". No sign-in UI: the API VALIDATES incoming JWT access tokens.
//
// Two authorization concepts, both wired below:
//   • SCOPE-or-APP-ROLE — one endpoint serves delegated (user) callers via scopes AND
//     app-only (daemon) callers via app roles, WITHOUT conflating the two.
//   • RESOURCE-BASED    — per-object "owner or elevated" decided at runtime.

using System.Security.Claims;
using EntraSsoSample.Api.Authorization;
using EntraSsoSample.Api.Models;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Identity.Web;

var builder = WebApplication.CreateBuilder(args);

// Validate bearer tokens issued by Entra for THIS API (audience = its own client id).
builder.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApi(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(options =>
{
    // Locked down by default: every endpoint needs a valid token unless it opts out
    // with .AllowAnonymous(). Fail safe, not fail open.
    options.FallbackPolicy = new AuthorizationPolicyBuilder()
        .RequireAuthenticatedUser()
        .Build();

    // "read" — delegated scope OR app role, either Read or ReadWrite grants read.
    options.AddPolicy("Reports.Read", p => p.Requirements.Add(
        new ScopeOrAppRoleRequirement(
            scopes:   ["Reports.Read", "Reports.ReadWrite"],
            appRoles: ["Reports.Read", "Reports.ReadWrite"])));

    // "write" — narrower: only ReadWrite.
    options.AddPolicy("Reports.Write", p => p.Requirements.Add(
        new ScopeOrAppRoleRequirement(
            scopes:   ["Reports.ReadWrite"],
            appRoles: ["Reports.ReadWrite"])));

    // Resource-based refinement (owner or elevated), checked in the endpoint.
    options.AddPolicy("ViewDocument", p => p.Requirements.Add(new SameOwnerOrElevatedRequirement()));
});

builder.Services.AddSingleton<IAuthorizationHandler, ScopeOrAppRoleHandler>();
builder.Services.AddSingleton<IAuthorizationHandler, DocumentAuthorizationHandler>();

var app = builder.Build();

app.UseAuthentication();
app.UseAuthorization();

// Public probe — no token required.
app.MapGet("/", () => Results.Ok(new
{
    status = "up",
    note = "Protected endpoints require an Entra bearer token (Authorization: Bearer <jwt>)."
})).AllowAnonymous();

// READ — open to delegated users (scope) and daemons (app role) alike.
app.MapGet("/api/reports", (ClaimsPrincipal user) => Results.Ok(new
{
    data = new[] { "Q1", "Q2", "Q3" },
    caller = CallerInfo.Describe(user),   // shows HOW you were authorized
})).RequireAuthorization("Reports.Read");

// WRITE — narrower policy (ReadWrite only).
app.MapPost("/api/reports", (ClaimsPrincipal user) => Results.Ok(new
{
    created = true,
    caller = CallerInfo.Describe(user),
})).RequireAuthorization("Reports.Write");

// RESOURCE-BASED — coarse policy gates the endpoint; the per-document check refines it.
app.MapGet("/api/documents/{id:int}", async (int id, ClaimsPrincipal user, IAuthorizationService authz) =>
{
    var doc = DocumentStore.Find(id);
    if (doc is null) return Results.NotFound();

    var decision = await authz.AuthorizeAsync(user, doc, "ViewDocument");
    return decision.Succeeded ? Results.Ok(doc) : Results.Forbid();
}).RequireAuthorization("Reports.Read");

app.Run();
