# infra — EasyAuth IaC samples

Sample IaC for both halves of an App Service EasyAuth setup. Validated with the
Bicep CLI (`az bicep build`).

## The Entra side is a script, not Bicep

`create-app-registration.sh` creates the app registration, the EasyAuth redirect
URI (`/.auth/login/aad/callback`), a service principal, and a client secret.
**Bicep cannot do this** — there is no Microsoft.Graph resource provider for
ARM/Bicep, so app registrations are made via az CLI, the portal, or Terraform's
`azuread` provider. The script prints the `tenantId` / `clientId` the Bicep needs.

```bash
./create-app-registration.sh "entra-sso-sample" "https://entra-sso-sample-dev.azurewebsites.net"
```

## The App Service side (`main.bicep`)

One deploy creates the App Service plan, the Linux .NET web app, the EasyAuth
`authsettingsV2` config (Entra as the identity provider, sign-in required), and the
`MICROSOFT_PROVIDER_AUTHENTICATION_SECRET` app setting that config references.

```bash
az group create -n rg-entra-sample -l eastus
az deployment group create -g rg-entra-sample -f main.bicep -p main.bicepparam \
  -p clientSecret=$(az keyvault secret show --vault-name <kv> -n easyauth-secret --query value -o tsv)
```

Then register the deployment's `redirectUri` output on the app registration (the
script already does this if you passed the final URL), and deploy your app code.

## Secrets

The client secret is never written to a file: the script prints it once (store it
in Key Vault), and `main.bicep` takes it as a `@secure` param injected at deploy,
then exposes it to EasyAuth by app-setting *name* — `authsettingsV2` references
`clientSecretSettingName`, never the value. For production, swap the app setting
for a Key Vault reference (commented in `main.bicep`).
