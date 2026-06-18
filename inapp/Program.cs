// In-app "full Entra" sample — authentication AND authorization live in this code.
//
// Auth (who you are):    OIDC sign-in (auth-code + PKCE) via Microsoft.Identity.Web,
//                        plus an access token used to call Microsoft Graph GET /me.
// Authz (what you can do): two flavors, both wired below —
//   • ROLE-BASED      — App Roles arrive in the "roles" claim; policies gate pages.
//   • RESOURCE-BASED  — "can you edit THIS document" is decided at runtime against
//                        the actual object, which no static claim can express.

using EntraSsoSample.InApp.Authorization;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// App Service terminates TLS and forwards the original scheme as X-Forwarded-Proto.
// Without honoring it, the app thinks requests are http and builds an http:// OIDC
// redirect_uri that won't match the registered https:// one (AADSTS redirect error).
builder.Services.Configure<ForwardedHeadersOptions>(o =>
{
    o.ForwardedHeaders = ForwardedHeaders.XForwardedProto | ForwardedHeaders.XForwardedFor;
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(
        options =>
        {
            builder.Configuration.GetSection("AzureAd").Bind(options);
            // Entra delivers App Role *values* in the "roles" claim. Tell ASP.NET Core to
            // treat that as the role claim so [Authorize(Roles=...)] and User.IsInRole() work.
            options.TokenValidationParameters.RoleClaimType = "roles";
        },
        // When a signed-in user lacks the required role, show a friendly page (403)
        // instead of the framework default path.
        cookieOptions => cookieOptions.AccessDeniedPath = "/AccessDenied")
    .EnableTokenAcquisitionToCallDownstreamApi(["User.Read"])
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    .AddInMemoryTokenCaches();

builder.Services.AddAuthorization(options =>
{
    // Require a signed-in user for every page by default.
    options.FallbackPolicy = options.DefaultPolicy;

    // ── Role-based policies (App Roles → "roles" claim) ──────────────────────────
    // Admin is listed in every policy because roles aren't a strict hierarchy unless
    // you make them one — so an Admin satisfies the lower policies explicitly.
    options.AddPolicy("RequireAdmin", p => p.RequireRole(Roles.Admin));
    options.AddPolicy("RequireApprover", p => p.RequireRole(Roles.Approver, Roles.Admin));
    options.AddPolicy("RequireAuditor", p => p.RequireRole(Roles.Auditor, Roles.Admin));
    options.AddPolicy("RequireContributor", p => p.RequireRole(Roles.Contributor, Roles.Admin));

    // Resource-based decisions don't get a named policy here — they're checked against a
    // specific Document via DocumentOperations.Read/Edit (see DocumentAuthorizationHandler).
});

// The handler that evaluates DocumentOperations against a specific Document instance.
builder.Services.AddSingleton<IAuthorizationHandler, DocumentAuthorizationHandler>();

builder.Services
    .AddRazorPages()
    .AddMicrosoftIdentityUI();

var app = builder.Build();

app.UseForwardedHeaders();  // must run before auth so the scheme is correct
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();  // order matters: authenticate before authorize
app.UseAuthorization();

app.MapRazorPages();

app.Run();
