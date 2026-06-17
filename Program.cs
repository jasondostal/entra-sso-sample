using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.Identity.Web;
using Microsoft.Identity.Web.UI;

var builder = WebApplication.CreateBuilder(args);

// ── The entire Entra SSO wiring — these three registrations are the whole story ──

// 1. Authenticate users with Entra via OpenID Connect (auth-code flow + PKCE).
//    Reads the "AzureAd" section of appsettings.json (Instance/TenantId/ClientId/…).
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

// 2. Require a signed-in user for every page by default (no [Authorize] needed).
builder.Services.AddAuthorization(options =>
{
    options.FallbackPolicy = options.DefaultPolicy;
});

// 3. Razor Pages + the built-in sign-in/sign-out UI from Microsoft.Identity.Web.
//    AddMicrosoftIdentityUI adds /MicrosoftIdentity/Account/{SignIn,SignOut}.
builder.Services
    .AddRazorPages()
    .AddMicrosoftIdentityUI();

var app = builder.Build();

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();  // order matters: authenticate before authorize
app.UseAuthorization();

app.MapRazorPages();

app.Run();
