# Entra ID setup — step-by-step walkthrough

How to create and configure the Entra ID (Azure AD) app registration that backs
this sample. Portal click-path first (good for following along), with the `az`
CLI fast-path at the end.

**Prerequisite:** you need permission to register apps in the tenant — the
*Application Developer* role (or higher), or "Users can register applications" =
Yes in tenant settings.

The end goal is three values for the app, plus correctly registered redirect URIs:

| Value | Where it goes |
|-------|---------------|
| Directory (tenant) ID | `AzureAd:TenantId` |
| Application (client) ID | `AzureAd:ClientId` |
| Client secret | user-secrets / Key Vault (never in a file) |

---

## 1. Create the app registration

1. [Entra admin center](https://entra.microsoft.com) → **Applications** → **App registrations** → **+ New registration**.
2. **Name:** something recognizable, e.g. `entra-sso-sample`.
3. **Supported account types:** *Accounts in this organizational directory only (single tenant)* — the simplest choice for an internal app.
4. **Redirect URI:** choose platform **Web**, value `https://localhost:5001/signin-oidc` (your local dev URL). You can add more later.
5. **Register.**

> **Web vs SPA vs Public client:** pick **Web**. A server-rendered ASP.NET app is a *confidential client* — it has a server that can keep a secret. SPA/Public-client are for browser/native apps that can't, and they change the token flow.

## 2. Copy the IDs

On the registration's **Overview** page, copy:
- **Application (client) ID** → `AzureAd:ClientId`
- **Directory (tenant) ID** → `AzureAd:TenantId`

These are identifiers, not secrets — safe to put in `appsettings.json`.

## 3. Redirect URIs

**Authentication** blade → **Add a platform** / edit the **Web** platform. The
redirect URI is where Entra sends the user back *after* sign-in, and it must match
the app's callback path **exactly** (scheme, host, port, path).

Which path depends on how the app does auth:

| Auth approach | Redirect URI to register |
|---------------|--------------------------|
| In-app (`Microsoft.Identity.Web`, this sample) | `https://<host>/signin-oidc` |
| App Service EasyAuth (platform) | `https://<host>/.auth/login/aad/callback` |

Add one entry per environment, e.g.:
- `https://localhost:5001/signin-oidc` (dev)
- `https://entra-sso-sample-dev.azurewebsites.net/signin-oidc` (deployed)

Also set **Front-channel logout URL** → `https://<host>/signout-callback-oidc`
so sign-out completes cleanly.

> **Implicit grant and hybrid flows:** tick **ID tokens**, leave **Access tokens**
> unticked. For *sign-in only*, `Microsoft.Identity.Web` uses the OpenID Connect
> hybrid flow (`response_type=id_token` returned via `form_post`), so Entra must be
> allowed to issue ID tokens. (You only move to pure authorization-code flow + PKCE
> when you also acquire access tokens for downstream APIs — then this box isn't
> needed.) The CLI script sets this with `--enable-id-token-issuance true`.

## 4. Client secret

**Certificates & secrets** → **Client secrets** → **+ New client secret**.
1. Description (e.g. `dev`), expiry (e.g. 6–12 months).
2. **Add**, then **copy the Value immediately** — it's shown only once.

Store it safely:
- **Dev:** `dotnet user-secrets set "AzureAd:ClientSecret" "<value>"`
- **Prod:** Key Vault, referenced from App Service app settings.

> Prefer a **certificate** or **federated credential** (workload identity) over a
> secret for anything long-lived — no expiry surprises, nothing to leak.

## 5. (Optional) Token configuration

For basic SSO you're done. If the app needs more user info in the token:
- **Token configuration** → **Add optional claim** (e.g. `email`, `family_name`).
- **API permissions** already includes `User.Read` (Microsoft Graph) by default,
  which is enough to sign in and read the basic profile.

## 6. Wire it into the app

In `appsettings.json` set `TenantId` and `ClientId` (from step 2). Set the secret
via user-secrets (step 4). That's all the app needs — see the repo
[README](../README.md).

## 7. Test the sign-in

`dotnet run`, browse to the HTTPS URL:
1. You're redirected to `login.microsoftonline.com`.
2. Sign in / consent.
3. Back to the app's home page, which lists your token claims.

### Common errors

| Error | Meaning | Fix |
|-------|---------|-----|
| `AADSTS50011: redirect URI ... does not match` | The app's callback URL isn't registered (or differs by port/scheme/trailing slash) | Add the **exact** URL to the Authentication blade |
| `AADSTS7000215: Invalid client secret` | Wrong/expired secret, or it wasn't loaded | Re-check user-secrets / KV value; create a new secret if expired |
| `AADSTS650056: Misconfigured application` | Consent/permissions issue | Grant admin consent under API permissions |
| Redirect loop / 403 after login | App got the token but authorization failed | Check the `FallbackPolicy` and that `UseAuthentication` precedes `UseAuthorization` |

---

## CLI fast-path

Everything above (steps 1–4) in one command — see
[`infra/create-app-registration.sh`](../infra/create-app-registration.sh):

```bash
./infra/create-app-registration.sh "entra-sso-sample" "https://entra-sso-sample-dev.azurewebsites.net"
```

It creates the app registration, registers both the in-app and EasyAuth redirect
URIs plus `https://localhost:5001/signin-oidc`, creates the service principal and a
1-year secret, and prints the `tenantId` / `clientId` / secret.

> Reminder: the app **registration** can only be made via CLI/portal/Terraform —
> Bicep has no Microsoft.Graph provider. The Bicep in `infra/` handles the App
> Service side once you have these IDs.
