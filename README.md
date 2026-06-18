# entra-sso-sample

Two runnable, side-by-side ways to put **Microsoft Entra ID single sign-on** in
front of a tiny ASP.NET Core web app — so you can *see* the difference between
letting the platform do auth and doing it yourself.

| | [`easyauth/`](easyauth/) | [`inapp/`](inapp/) |
|---|---|---|
| **Who signs the user in** | App Service "Authentication" (EasyAuth), *in front of* the app | The app itself (`Microsoft.Identity.Web`) |
| **Auth code in the app** | **None** — reads `X-MS-CLIENT-PRINCIPAL-*` headers | OpenID Connect wired in `Program.cs` |
| **NuGet packages** | none | `Microsoft.Identity.Web` (+ `.UI`, `.GraphServiceClient`) |
| **What you get to see** | the injected identity headers + claims | the **full OAuth2 round trip**: sign-in → access token → call Microsoft Graph |
| **Runs locally?** | no (EasyAuth only exists on App Service) | **yes** — full flow against your real tenant from `localhost` |
| **Downstream API calls** | not without extra work | **yes** — `GET /me` on Microsoft Graph with an access token |
| **Hosting** | F1 (Free) | F1 (Free), or localhost for $0 |

Both are protected by the same Entra tenant. EasyAuth is the no-code, fastest path;
the in-app variant is the one to **play with** when you want to understand tokens,
scopes, consent, and calling downstream APIs — the machinery EasyAuth hides.

## Cost (the short version)

For workforce sign-in in your own tenant, **"full Entra" is essentially free**:
app registrations, OIDC sign-ins, token issuance, and Microsoft Graph calls are all
**Entra ID Free**, and both samples run on **F1 (Free)** App Service. The cheapest
way to explore the complete in-app flow is to run it on **localhost** against your
real tenant — $0 of Azure compute. Full breakdown (and the secret-vs-federated
trade-off) in [`docs/COST.md`](docs/COST.md).

## EasyAuth pattern — [`easyauth/`](easyauth/)

```
browser ──▶ App Service "Authentication" ──▶ your app
              │  (does the Entra OIDC sign-in)   ▲
              │  injects X-MS-CLIENT-PRINCIPAL-* │
              └──────────────────────────────────┘
```

The app has **zero** authentication code; its home page just dumps the identity
headers EasyAuth injects. See [`easyauth/`](easyauth/) and its
[`infra/`](easyauth/infra/).

## In-app pattern — [`inapp/`](inapp/)

```
browser ──▶ your app (Microsoft.Identity.Web)
              │  1. redirects to Entra, signs the user in (auth-code + PKCE)
              │  2. acquires an ACCESS token for scope User.Read
              └▶ 3. calls Microsoft Graph GET /me with that token
```

The home page shows both the ID-token claims **and** the Graph `/me` result, so the
whole sign-in → token → API-call chain is visible on one page. See [`inapp/`](inapp/)
and its [`infra/`](inapp/infra/).

## Setup, end to end

Entra app registration walkthrough (shared concepts) lives in
[`docs/ENTRA-SETUP.md`](docs/ENTRA-SETUP.md). Each variant's `infra/README.md` has
its exact `create-app-registration.sh` + Bicep deploy steps — the two differ in
redirect URI (`/.auth/login/aad/callback` vs `/signin-oidc`) and in whether they
request Graph `User.Read`.
