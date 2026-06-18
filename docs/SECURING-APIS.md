# Securing apps and APIs in an Entra shop — the model to standardize on

Written for a team about to stand up many APIs fast. The goal is one authorization
model everyone copies, so authz doesn't get reinvented (badly) per service.

## TL;DR decision tree

- **"What kind of user is this?"** → **App Roles** (the `roles` claim). Policy-based.
- **"Can this caller hit this API at all / this operation?"** → **scopes** (delegated)
  **or app roles** (app-only). Never conflate the two.
- **"Can this caller act on THIS specific object?"** → **resource-based authorization**
  (a runtime check against the object). No claim can answer this.
- **Groups** → use them to *administer* role assignment, not as your app's authz primitive.

## Why not "AD groups in the token"?

Putting group membership in the token and mapping groups → roles in your app is the
instinct everyone has. It works in a demo and bites you in production for two reasons:

1. **Token overage.** Entra refuses to bloat tokens: past **200 groups** (JWT) — **150**
   for SAML — it *drops the groups claim* and instead returns a `_claim_names` /
   `_claim_sources` pointer, expecting you to call Graph (`/me/transitiveMemberOf`) to
   enumerate. Senior people are in the most groups, so your app breaks for exactly them.
2. **Coupling.** Group GUIDs are opaque, owned and reorganized by whoever runs AD. Your
   authorization ends up holding a hand-maintained `{guid → role}` table that drifts.

Authorization is an **app** concern; group taxonomy is an **org** concern. Don't weld them.

## The pattern: App Roles, with AD groups assigned to them

- Define `appRoles` on the **app registration** (your vocabulary: `Reader`, `Approver`,
  `Admin`, `Reports.ReadWrite`, …). You own these; they don't change when AD reorganizes.
- In the **Enterprise App → Users and groups**, assign **AD groups** to those roles. Admins
  keep managing access the familiar way (drop a user in a group); your app never sees a GUID.
- Entra stamps a small `roles` claim of your role *values*. No overage, no mapping table.

```csharp
// Program.cs
options.AddPolicy("RequireAdmin",    p => p.RequireRole("Admin"));
options.AddPolicy("RequireApprover", p => p.RequireRole("Approver", "Admin"));
// [Authorize(Policy = "RequireAdmin")]  — or  User.IsInRole("Admin")
```

For roles to drive `IsInRole`/`RequireRole`, set the role claim type (Entra uses `roles`):

```csharp
.AddMicrosoftIdentityWebApp(o =>
{
    Configuration.GetSection("AzureAd").Bind(o);
    o.TokenValidationParameters.RoleClaimType = "roles";
})
```

### The one real caveat — nested groups

Assigning a **group** to an app role grants the role to that group's **direct members
only** — it does **not** expand nested/transitive groups. If your org leans on nested
groups, either assign the leaf groups people are direct members of, resolve transitive
membership via Graph (`transitiveMemberOf`), or fall back to the groups claim **restricted
to groups assigned to the application** (which keeps it small and dodges overage).

## APIs: delegated scopes vs application app-roles

A protected API gets two *different* kinds of token, and they authorize differently:

| | Delegated (a user signed in) | App-only (a daemon / service) |
|---|---|---|
| Flow | auth-code / OBO — there is a user | client credentials — no user |
| Permission claim | **`scp`** (scopes, space-delimited) | **`roles`** (app roles) |
| Granted via | user/admin **consent** to a scope | admin grants an **Application permission** |
| `oid` present? | yes (the user) | no |

The mistake to avoid: treating a delegated scope and an app role as interchangeable. A
delegated token should be authorized on **scopes**; an app-only token on **app roles** —
never let one stand in for the other. The reference handler does exactly this:

```csharp
// ScopeOrAppRoleHandler — one endpoint, both caller types, kept distinct
var scopes = user.Scopes();           // "scp"
if (scopes.Count > 0)                  // delegated token
    => succeed if required scope present;  // and STOP (don't consult roles)
else                                    // app-only token
    => succeed if required app role present;
```

Define the API's scopes (`oauth2PermissionScopes`) and app roles (member type
`Application`) on its app registration; see [`../api/infra/`](../api/infra/).

## Resource-based authorization (per-object)

Roles and scopes are coarse: "can this *type* of caller do this *kind* of thing." The
moment you need "can Jason approve *this* $2M loan," that is **not** a token claim — push
it to a runtime decision against the object:

```csharp
var decision = await _authz.AuthorizeAsync(User, theDocument, "EditDocument");
if (!decision.Succeeded) return Forbid();   // 403 — and re-check on every mutation
```

Implemented with `AuthorizationHandler<TRequirement, TResource>` — the handler sees the
actual object (owner, status, tenant, …) and decides. The endpoint's role/scope policy
still runs first as a coarse gate; the resource check refines it. Live demos:
[`../inapp/`](../inapp/) (web) and [`../api/`](../api/) (API).

## Standardize this, then template it

For a fleet of fast-arriving APIs, the win is making this the **default** so no team
hand-rolls it: a shared authz library (`ScopeOrAppRoleRequirement` + handler, the role
claim-type config, a resource-based base handler) plus a project template that wires
`AddMicrosoftIdentityWebApi`, the `FallbackPolicy`, and the standard policies out of the
box. New API = inherit the baseline, define its scopes/roles/resource rules, done.

## Checklist for a new API

- [ ] `AddMicrosoftIdentityWebApi`; `FallbackPolicy` requires an authenticated caller (fail safe).
- [ ] App registration exposes **scopes** (delegated) and/or **app roles** (app-only) it needs.
- [ ] Policies authorize **scope-or-app-role**, keeping the two models distinct.
- [ ] Per-object rules use **resource-based** handlers, not claims.
- [ ] Roles come from **App Roles** (AD groups assigned to them) — not raw group GUIDs.
- [ ] No secrets in code; secret/cert in Key Vault or a federated credential.
