# infra — Entra SSO IaC samples

Sample IaC for both halves of the setup. Validated with Bicep CLI (`az bicep build`).

## The Entra side is a script, not Bicep

`create-app-registration.sh` creates the app registration, redirect URIs, service
principal, and a client secret. **Bicep cannot do this** — there is no
Microsoft.Graph resource provider for ARM/Bicep, so app registrations are made via
az CLI, the portal, or Terraform's `azuread` provider. The script prints the
`tenantId` / `clientId` that the Bicep below consumes.

```bash
./create-app-registration.sh "entra-sso-sample" "https://entra-sso-sample-dev.azurewebsites.net"
```

## Two App Service auth styles — pick one

| File | Auth lives in | When |
|------|---------------|------|
| `main.bicep` | **the app code** (`Microsoft.Identity.Web`) | matches the sample app in this repo; portable, explicit |
| `easyauth.bicep` | **the platform** (App Service "Authentication") | zero app code; sign-in handled in front of the app |

### In-app (`main.bicep`)

Deploys an App Service plan + Linux .NET web app and wires the `AzureAd__*` app
settings the app reads. No auth-specific Azure resource — the app does it.

```bash
az group create -n rg-entra-sample -l eastus2
az deployment group create -g rg-entra-sample -f main.bicep -p main.bicepparam \
  -p clientSecret=$(az keyvault secret show --vault-name <kv> -n entra-sample-secret --query value -o tsv)
```

Then register the deployment's `redirectUri` output on the app registration.

### EasyAuth (`easyauth.bicep`)

Applies an `authsettingsV2` config to an **existing** web app. Set the secret app
setting first, then deploy:

```bash
az webapp config appsettings set -g rg-entra-sample -n <app> \
  --settings MICROSOFT_PROVIDER_AUTHENTICATION_SECRET=$(az keyvault secret show --vault-name <kv> -n entra-sample-secret --query value -o tsv)

az deployment group create -g rg-entra-sample -f easyauth.bicep \
  -p siteName=<app> tenantId=<tid> clientId=<cid>
```

EasyAuth's redirect URI is `https://<app>/.auth/login/aad/callback` (the script
registers both that and `/signin-oidc`, so either variant works).

## Secrets

The client secret is never written to a file: the script prints it once (store it
in Key Vault), `main.bicep` takes it as a `@secure` param injected at deploy, and
`easyauth.bicep` references it by app-setting *name*, not value.
