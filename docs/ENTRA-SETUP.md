# Entra ID setup for EasyAuth — step-by-step walkthrough

How to create and configure the Entra ID (Azure AD) app registration that backs
**App Service EasyAuth** for this sample. Portal click-path first, with the `az`
CLI fast-path at the end.

**Prerequisite:** permission to register apps in the tenant — the *Application
Developer* role (or higher), or "Users can register applications" = Yes.

The end goal is three values plus one correctly registered redirect URI:

| Value | Where it goes |
|-------|---------------|
| Directory (tenant) ID | `main.bicep` param `tenantId` |
| Application (client) ID | `main.bicep` param `clientId` |
| Client secret | Key Vault → app setting `MICROSOFT_PROVIDER_AUTHENTICATION_SECRET` |

> **Shortcut:** the App Service **Authentication** blade has an *Express* option that
> creates the app registration for you. This walkthrough does it explicitly so you
> can see (and IaC) every piece — which is what `infra/` does.

---

## 1. Create the app registration

1. [Entra admin center](https://entra.microsoft.com) → **Applications** → **App registrations** → **+ New registration**.
2. **Name:** something recognizable, e.g. `entra-sso-sample`.
3. **Supported account types:** *Accounts in this organizational directory only (single tenant)*.
4. **Redirect URI:** platform **Web**, value `https://<your-app>.azurewebsites.net/.auth/login/aad/callback`.
5. **Register.**

> **The EasyAuth redirect URI is always `/.auth/login/aad/callback`** — that's the
> endpoint App Service itself exposes to receive the sign-in response. (This differs
> from in-app auth, which would use `/signin-oidc`.)

## 2. Copy the IDs

On the registration's **Overview** page, copy:
- **Application (client) ID** → `clientId`
- **Directory (tenant) ID** → `tenantId`

These are identifiers, not secrets.

## 3. Client secret

**Certificates & secrets** → **Client secrets** → **+ New client secret**.
1. Description (e.g. `easyauth`), expiry (e.g. 6–12 months).
2. **Add**, then **copy the Value immediately** — shown only once.

Store it in Key Vault. App Service reads it from the app setting
`MICROSOFT_PROVIDER_AUTHENTICATION_SECRET` (set by `main.bicep`, ideally as a Key
Vault reference). It is never put in code or committed.

> Prefer a **certificate** or **federated credential** over a secret for anything
> long-lived — no expiry surprises.

## 4. Turn on EasyAuth (App Service Authentication)

You can do this in IaC with [`infra/main.bicep`](../infra/main.bicep) (recommended),
or in the portal:

1. App Service → **Settings** → **Authentication** → **Add identity provider**.
2. **Identity provider:** Microsoft.
3. **App registration type:** *Provide the details of an existing app registration*.
4. Paste the **client ID**, **issuer URL** `https://login.microsoftonline.com/<tenantId>/v2.0`, and the **client secret**.
5. **Restrict access:** *Require authentication*.
6. **Unauthenticated requests:** *HTTP 302 Redirect to login page*.
7. **Add.**

That's the equivalent of the `authsettingsV2` block in `main.bicep`.

## 5. Test the sign-in

Browse to the app URL:
1. EasyAuth redirects you to `login.microsoftonline.com`.
2. Sign in / consent.
3. Back to the app — the home page lists your identity and claims, read from the
   `X-MS-CLIENT-PRINCIPAL-*` headers EasyAuth injected.

Sign out via `https://<app>/.auth/logout`. You can inspect the raw principal any
time at `https://<app>/.auth/me`.

### Common errors

| Error | Meaning | Fix |
|-------|---------|-----|
| `AADSTS50011: redirect URI ... does not match` | The `/.auth/login/aad/callback` URI isn't registered (or host differs) | Add the **exact** URL to the app registration's Authentication blade |
| `AADSTS7000215: Invalid client secret` | Wrong/expired secret, or the app setting isn't set | Re-check `MICROSOFT_PROVIDER_AUTHENTICATION_SECRET` / KV value; rotate if expired |
| `AADSTS650056: Misconfigured application` | Consent/permissions issue | Grant admin consent under API permissions |
| Endless redirect loop | Issuer/tenant mismatch in the EasyAuth config | Verify the issuer URL tenant GUID matches the app reg's tenant |

---

## CLI fast-path

Steps 1–3 in one command — see
[`infra/create-app-registration.sh`](../infra/create-app-registration.sh):

```bash
./infra/create-app-registration.sh "entra-sso-sample" "https://entra-sso-sample-dev.azurewebsites.net"
```

It creates the app registration, registers the `/.auth/login/aad/callback` redirect
URI, creates the service principal and a 1-year secret, and prints the `tenantId` /
`clientId` / secret to feed into `main.bicep`.

> Reminder: the app **registration** can only be made via CLI/portal/Terraform —
> Bicep has no Microsoft.Graph provider. `main.bicep` handles the App Service +
> EasyAuth config once you have these IDs.
