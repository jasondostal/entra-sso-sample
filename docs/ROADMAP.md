# Roadmap — make this authz model the platform default

This sample proves the model. The real win is teams not hand-rolling it. The goal:
a new API inherits "authn + authz done right" out of the box, and the gates prove it.
This file is the running TODO for folding the model into the Azure platform cascade
(`azure-platform-iac` → `azure-project-starter` → Secure Coding Platform).

> Status: **idea / not started.** Captured 2026-06-18 from the entra-sso-sample build.

## What "done" looks like

```
azure-project-starter  --generate-->  new dotnet-api repo that ALREADY has:
  • JWT bearer validation, FallbackPolicy (fail-safe)        ← from shared lib
  • scope-or-app-role policies + resource-based base handler ← from shared lib
  • its app registration with the right scopes/app-roles     ← from app-reg automation
  • a CI gate that fails if an endpoint has no authz         ← from Secure Coding Platform
```

## Phase 1 — extract a shared authz library

Pull the patterns out of `api/` and `inapp/` into a versioned NuGet package
(`Fox.Security.Authorization` or similar), owned by the platform team.

- [ ] `ScopeOrAppRoleRequirement` + handler (delegated `scp` vs app-only `roles`, kept distinct).
- [ ] Resource-based base: `OperationAuthorizationRequirement` conventions + a generic owner/tenant handler to subclass.
- [ ] `AddEntraApiSecurity(...)` extension: `AddMicrosoftIdentityWebApi` + `FallbackPolicy` + `RoleClaimType="roles"` + standard policy registration, one call.
- [ ] `Roles` / `Scopes` constants conventions (the single customization point per service).
- [ ] Unit tests for the handlers (delegated-only, app-only, missing-claim, overage-fallback).
- [ ] Ship to the internal feed; semver; changelog.

## Phase 2 — wire it into the archetypes

`azure-project-starter` already emits archetype-specific source and references the
platform pipeline templates. Make security part of the generated baseline.

- [ ] `dotnet-api` archetype: reference the authz package; generate `Program.cs` wired via `AddEntraApiSecurity`; ship a sample protected + resource-based endpoint.
- [ ] `dotnet-web` archetype: the interactive (OIDC) variant — App Roles policies + `AccessDenied` page, like `inapp/`.
- [ ] Cookiecutter variables: `{{ exposed_scopes }}`, `{{ app_roles }}` so a new service declares its vocabulary at generation time.
- [ ] `.cruft.json` so existing services pull authz updates when the template moves.

## Phase 3 — automate the Entra side

Bicep can't make app registrations, so the post-gen hook / an infra pipeline stage owns it.

- [ ] Extend the post-gen hook (it already generates stable app-reg GUIDs) to also create `appRoles` and `oauth2PermissionScopes` from the cookiecutter vars.
- [ ] **Group-assignment convention** (the part Jason likes): each role maps to a named AD group (`APP-<svc>-<Role>`); the pipeline assigns the group to the app role, so access is administered by group membership, never group GUIDs in code. Document the nested-group caveat (direct members only).
- [ ] Idempotent + reversible (disable-before-remove when roles change) — mirror the dance in `inapp/infra/create-app-registration.sh`.
- [ ] Secret/cert via Key Vault or a federated credential; no secrets in pipelines.

## Phase 4 — prove it in the gates (Secure Coding Platform)

Make "secured correctly" enforced, not aspirational. Fits the existing shift-left ladder.

- [ ] Build-stage analyzer/test: fail if a controller/endpoint has neither `[Authorize]`/policy nor an explicit `[AllowAnonymous]` (no silent-public endpoints).
- [ ] Convention check: delegated and app-only permissions aren't conflated; resource ops go through the resource handler.
- [ ] Feed findings into the existing PR-stage LLM review (warning-only) as an authz lens.
- [ ] Audit story: the gate config is itself versioned + audited (the regulatory play).

## Open questions / decisions to make

- **One shared package vs source generator vs template-only?** A package centralizes fixes but couples versions; template-only avoids coupling but drifts. Leaning package + cruft.
- **Role taxonomy:** per-service roles (this sample) vs a small shared enterprise set vs both. Probably per-service roles + a few cross-cutting ones (e.g. `Auditor`).
- **Groups source of truth:** who owns `APP-<svc>-<Role>` group lifecycle — platform, or each app team via a request flow?
- **External ID / B2C:** if any API ever faces customers, that's a different identity store (per-MAU) — keep workforce and customer planes separate from day one.

## Related

- The model writeup: [`SECURING-APIS.md`](SECURING-APIS.md)
- Live web RBAC + resource-based demo: [`../inapp/`](../inapp/)
- API template: [`../api/`](../api/)
- Cost context: [`COST.md`](COST.md)
