# entra-sso-sample

A minimal, runnable example of **Microsoft Entra ID single sign-on for a web app using App Service EasyAuth** — the platform/no-code approach. App Service runs the Entra sign-in *in front of* the app; the app contains **zero authentication code** and just reads the identity the platform injects.

> This is the **EasyAuth** pattern. The alternative is in-app auth with
> `Microsoft.Identity.Web` (OpenID Connect in your own code) — not covered here.

## How EasyAuth works

```
browser ──▶ App Service "Authentication" ──▶ your app
              │  (does the Entra OIDC sign-in)   ▲
              │  injects X-MS-CLIENT-PRINCIPAL-* │
              └──────────────────────────────────┘
```

1. An unauthenticated request hits App Service; EasyAuth redirects it to Entra to sign in.
2. After sign-in, EasyAuth forwards the request to the app with the user attached as
   request headers (`X-MS-CLIENT-PRINCIPAL-NAME`, `X-MS-CLIENT-PRINCIPAL-ID`, and the
   full base64 `X-MS-CLIENT-PRINCIPAL` claims blob).
3. The app reads those headers. No auth libraries, no token handling, no secrets in code.

## The app

A tiny Razor Pages app whose home page **dumps the EasyAuth identity headers and
claims** — a clean way to *see* EasyAuth working. The entire app:

- `Program.cs` — a plain web app. No authentication code, no auth NuGet packages.
- `Pages/Index.cshtml.cs` — reads `X-MS-CLIENT-PRINCIPAL-*` and decodes the claims.
- Sign-out is the built-in `/.auth/logout` endpoint (no app code or routes).

## Setup, end to end

1. **Register the app in Entra** + set the redirect URI + create a client secret —
   full walkthrough in [`docs/ENTRA-SETUP.md`](docs/ENTRA-SETUP.md), or one command
   via [`infra/create-app-registration.sh`](infra/create-app-registration.sh).
   The EasyAuth redirect URI is `https://<host>/.auth/login/aad/callback`.
2. **Deploy** the App Service + EasyAuth config with [`infra/main.bicep`](infra/main.bicep)
   (see [`infra/README.md`](infra/README.md)).
3. **Deploy the app code** (e.g. `az webapp deploy`), browse to the URL, and you'll be
   bounced to Entra to sign in, then back to the claims page.

## Running locally

EasyAuth only exists on App Service, so locally there are no identity headers —
`dotnet run` works but the home page will say "no EasyAuth headers found." That's
expected; the auth only kicks in once deployed behind App Service.

## Infrastructure-as-code

The [`infra/`](infra/) folder has the matching IaC:
- `create-app-registration.sh` — the Entra app registration + redirect URI + secret
  (an `az` script, because **Bicep can't create app registrations**).
- `main.bicep` — App Service plan + web app + the EasyAuth `authsettingsV2` config +
  the client-secret app setting it references. One deploy, fully protected.

Bicep validated with `az bicep build`. See [`infra/README.md`](infra/README.md).
