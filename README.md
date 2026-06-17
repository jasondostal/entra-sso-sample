# entra-sso-sample

A minimal, runnable example of **Microsoft Entra ID single sign-on in an ASP.NET Core web app** — the in-app `Microsoft.Identity.Web` approach (OpenID Connect, auth-code flow + PKCE). Built to *show* the pattern: the entire auth wiring is three registrations in `Program.cs`, and the home page prints your ID-token claims once you're signed in.

> This is the **code-level** SSO pattern. The no-code alternative is App Service **EasyAuth**, which puts sign-in in front of the app at the platform layer with no app changes.

## The whole auth story

It's just `Program.cs`:

```csharp
builder.Services
    .AddAuthentication(OpenIdConnectDefaults.AuthenticationScheme)
    .AddMicrosoftIdentityWebApp(builder.Configuration.GetSection("AzureAd"));

builder.Services.AddAuthorization(o => o.FallbackPolicy = o.DefaultPolicy); // require sign-in everywhere

builder.Services.AddRazorPages().AddMicrosoftIdentityUI();                  // adds sign-in/out endpoints
```

…plus `app.UseAuthentication()` / `app.UseAuthorization()` in the pipeline.

## 1. Register the app in Entra

Entra ID → **App registrations** → **New registration**:

- **Supported account types:** *Accounts in this organizational directory only* (single-tenant).
- **Redirect URI:** platform **Web** → `https://localhost:5001/signin-oidc`.
- After creating, copy the **Application (client) ID** and **Directory (tenant) ID**.
- **Certificates & secrets** → **New client secret** → copy the value.

In the **Authentication** blade, also register the sign-out URL (Front-channel logout): `https://localhost:5001/signout-callback-oidc`. With auth-code + PKCE you do **not** need to tick the implicit-flow "ID tokens" box.

| Purpose | Redirect URI | Config key |
|---|---|---|
| Sign-in callback | `https://localhost:5001/signin-oidc` | `CallbackPath` |
| Sign-out callback | `https://localhost:5001/signout-callback-oidc` | `SignedOutCallbackPath` |

## 2. Configure

Put your IDs in `appsettings.json` (`TenantId`, `ClientId`). Keep the **secret out of source** — use user-secrets in dev:

```bash
dotnet user-secrets set "AzureAd:ClientSecret" "<the-secret-value>"
```

In production set `AzureAd__ClientSecret` as an app setting or pull from Key Vault.

## 3. Run

```bash
dotnet run
```

Browse to the HTTPS URL it prints → you'll be redirected to Entra to sign in → back to the home page, which lists your claims. Sign out with the header link.

## Infrastructure-as-code

The [`infra/`](infra/) folder has matching IaC samples:
- `create-app-registration.sh` — the Entra app registration + redirect URIs + secret (an `az` script, because **Bicep can't create app registrations**).
- `main.bicep` — App Service for **this** in-app auth approach (wires the `AzureAd__*` app settings).
- `easyauth.bicep` — the **EasyAuth** alternative (`authsettingsV2`, no app code).

All Bicep validated with `az bicep build`. See [`infra/README.md`](infra/README.md).

## Notes

- **MVC instead of Razor Pages:** swap `AddRazorPages().AddMicrosoftIdentityUI()` for `AddControllersWithViews().AddMicrosoftIdentityUI()` and map controller routes. Everything else is identical.
- **Calling Graph / downstream APIs:** chain `.EnableTokenAcquisitionToCallDownstreamApi(...)` — not needed for pure SSO.
