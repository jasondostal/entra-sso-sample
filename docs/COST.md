# What does "full Entra" actually cost?

Short answer: **for workforce sign-in in your own tenant, basically nothing.** The
fear that "real Entra auth costs money" comes from conflating three different
products. Here's the breakdown.

## The pieces

| Thing | Cost | Needed here? |
|---|---|---|
| **Entra ID Free** — app registrations, OIDC/OAuth2 sign-in, token issuance, Microsoft Graph calls for users **in your own tenant** | **$0**, unlimited apps | ✅ this is all either sample needs |
| **App Service hosting** | **F1 (Free)** runs both samples | ✅ but $0 on F1 |
| **Entra External ID / Azure AD B2C** (consumer/customer identity, per-MAU) | Free to 50k MAU, then paid | ❌ that's customer identity, not workforce SSO |
| **Entra ID P1 / P2** (Conditional Access, PIM, risk policies) | Paid per user | ❌ not needed for plain OIDC |
| **Key Vault** (if you store the client secret there) | a few cents/month | optional |

So the "full Entra" experience — sign-in, tokens, scopes, consent, Graph — is free
to learn and free to run small.

## The cheapest way to play with the *complete* flow

Run the in-app sample **on localhost against your real Entra tenant**. Entra does
not care that the redirect URI is `http://localhost:5000/signin-oidc`; the entire
OAuth2 dance (auth-code + PKCE, access-token acquisition, the consent screen, the
Graph call) works identically to production — for **$0 of Azure compute**.

```bash
cd inapp
# one-time: register a localhost redirect URI
./infra/create-app-registration.sh "entra-inapp-local" "http://localhost:5000"
# put TenantId/ClientId in appsettings.json, secret in user-secrets:
dotnet user-secrets set "AzureAd:ClientSecret" "<the-secret>"
dotnet run
```

Deploy to F1 only when you want to see it live behind a real `*.azurewebsites.net`
hostname.

## The one design choice that touches cost

**Client secret vs. secret-less federated credential.**

- **Client secret** (what both samples use): dead simple, works on localhost and on
  **F1 Free**. You rotate the secret yourself (the script sets a 1-year expiry).
- **Federated / workload identity** (no secret to manage): the app authenticates as
  its **managed identity** instead of presenting a secret. Cleaner and more secure —
  but App Service managed identity requires **Basic (B1) or higher** (~$13/mo), so
  it's not free.

For a playground, **client-secret-on-F1 (or localhost)** is the cost-conscious pick.
Graduate to a federated credential + B1 when you want production hygiene.

## What would actually start a meter

- Moving off F1 to **B1+** (managed identity, Always On, custom domain TLS, slots).
- Adding **Key Vault** (pennies) or **Application Insights** (ingestion-based).
- Switching to **External ID / B2C** for *customer* sign-up/sign-in past the free MAU.
- Buying **Entra P1/P2** for Conditional Access / PIM.

None of those are required to run, learn, or demo either sample in this repo.
