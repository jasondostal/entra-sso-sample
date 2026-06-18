// In-app "full Entra" sample — the authentication lives in THIS code, not the
// platform. Contrast with ../easyauth, where App Service does the sign-in and the
// app has zero auth code.
//
// What this wires up, in one fluent chain:
//   1. AddMicrosoftIdentityWebApp ............ OpenID Connect sign-in (auth-code + PKCE)
//   2. EnableTokenAcquisitionToCallDownstreamApi  acquire ACCESS tokens for scopes
//   3. AddMicrosoftGraph ..................... a GraphServiceClient pre-wired with those tokens
//   4. AddInMemoryTokenCaches ................ where the acquired tokens are cached
// Together that's the whole OAuth2 picture EasyAuth hides: sign-in -> token -> call an API.

using Microsoft.AspNetCore.Authentication.OpenIdConnect;
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
    // The proxy IP isn't known ahead of time on App Service — trust the forwarded headers.
    o.KnownIPNetworks.Clear();
    o.KnownProxies.Clear();
});

builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    // Sign users in with Entra. Reads the "AzureAd" config section
    // (Instance / TenantId / ClientId / ClientSecret / callback paths).
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"))
    // On top of sign-in, also acquire access tokens for downstream APIs.
    // The initial scope is consented at sign-in; more can be added incrementally.
    .EnableTokenAcquisitionToCallDownstreamApi(["User.Read"])
    // Hand us a GraphServiceClient that automatically attaches those tokens.
    // Reads the "MicrosoftGraph" config section (BaseUrl + Scopes).
    .AddMicrosoftGraph(builder.Configuration.GetSection("MicrosoftGraph"))
    // Tokens have to live somewhere. In-memory is fine for a sample / single instance;
    // use a distributed cache (Redis, etc.) for real multi-instance apps.
    .AddInMemoryTokenCaches();

// Require a signed-in user for every page by default — no [Authorize] sprinkling.
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// Razor Pages + the built-in sign-in/sign-out UI (/MicrosoftIdentity/Account/...).
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
