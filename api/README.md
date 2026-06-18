# api — protected Web API (the template for your APIs)

A token-validating ASP.NET Core Web API secured by Microsoft Entra. This is the
shape to copy when standing up new APIs: no sign-in UI, it just validates the
**JWT access token** on every request and authorizes on what the token carries.

## The three things it demonstrates

1. **JWT bearer validation** — `AddMicrosoftIdentityWebApi`. Locked down by default
   (a `FallbackPolicy` requires a valid token; endpoints opt out with `AllowAnonymous`).
2. **Scope-or-app-role** — the bit people get wrong. A *delegated* token (a user
   signed in) carries **scopes** in `scp`; an *app-only* token (a daemon) carries
   **app roles** in `roles`. They are different permission models and must not be
   conflated. `ScopeOrAppRoleHandler` authorizes a delegated call on scopes **only**
   and an app-only call on app roles **only** — one endpoint, both caller types.
3. **Resource-based authorization** — the endpoint policy is coarse (`Reports.Read`);
   `GET /api/documents/{id}` then refines it per-object: you see a document only if you
   **own** it or are **elevated** (`Reports.ReadWrite`). No claim can express that — it's
   decided at runtime against the actual resource.

## Endpoints

| Method + path | Policy | Who passes |
|---|---|---|
| `GET /` | anonymous | anyone (probe) |
| `GET /api/reports` | `Reports.Read` | scope **or** app role `Reports.Read`/`ReadWrite` |
| `POST /api/reports` | `Reports.Write` | scope **or** app role `Reports.ReadWrite` |
| `GET /api/documents/{id}` | `Reports.Read` **+** per-doc `ViewDocument` | owner, or elevated |

`/api/reports` echoes a `caller` object so you can see whether the token was
delegated or app-only and which scopes/roles it carried.

## Run / verify locally

```bash
dotnet run --project Api.csproj
# no token  -> 401 (Bearer challenge); anonymous '/' -> 200
curl -i http://localhost:5099/api/reports     # 401
curl -i http://localhost:5099/                # 200
```

To exercise the authorized paths you need a real Entra token — see
[`infra/README.md`](infra/README.md) for how to mint delegated and app-only tokens.

## Deploy

See [`infra/`](infra/): `create-app-registration.sh` (exposes the scopes + app roles)
then `main.bicep` (F1, free). The reference web app that calls APIs like this is in
[`../inapp/`](../inapp/); the authorization model is written up in
[`../docs/SECURING-APIS.md`](../docs/SECURING-APIS.md).
