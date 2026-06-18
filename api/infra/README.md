# infra — protected Web API

IaC for the token-validating Web API. Validated with `az bicep build`.

## The Entra side (`create-app-registration.sh`)

Creates the API app registration with an **Application ID URI** (`api://<clientId>`),
two delegated **scopes** (`Reports.Read`, `Reports.ReadWrite`), and two app-only
**app roles** (`Reports.Read`, `Reports.ReadWrite`). No redirect URI, no secret — the
API only validates tokens.

```bash
./create-app-registration.sh "entra-api-sample"
```

## The App Service side (`main.bicep`)

F1 (Free) Linux web app + the `AzureAd__*` app settings the API reads. No secret.

```bash
az group create -n rg-entra-api -l eastus
az deployment group create -g rg-entra-api -f main.bicep -p main.bicepparam
```

## Getting a token to call it

- **Delegated (a user):** a client app (SPA/web) requests scope
  `api://<clientId>/Reports.Read`; the user's access token carries `scp: Reports.Read`.
- **App-only (a daemon):** register a *client* app, under its API permissions add the
  API's **Application permission** `Reports.ReadWrite`, admin-consent, then do a
  client-credentials grant for resource `api://<clientId>` — the token carries
  `roles: ["Reports.ReadWrite"]` and no `scp`.

The API's `caller` field in `/api/reports` echoes which kind it saw.
