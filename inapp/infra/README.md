# infra — in-app (Microsoft.Identity.Web) IaC

Sample IaC for the in-app auth variant. Validated with the Bicep CLI
(`az bicep build`).

## The Entra side is a script, not Bicep

`create-app-registration.sh` creates the app registration, the OIDC redirect URI
(`/signin-oidc`), delegated Microsoft Graph **User.Read**, a service principal, and
a client secret. **Bicep cannot do this** — there is no Microsoft.Graph resource
provider for ARM/Bicep. The script prints the `tenantId` / `clientId` the Bicep needs.

```bash
./create-app-registration.sh "entra-inapp-sample" "https://entra-inapp-sample-dev.azurewebsites.net"
```

## The App Service side (`main.bicep`)

One deploy creates the App Service plan (**F1 / Free by default**), the Linux .NET
web app, and the `AzureAd__*` app settings the app reads. No EasyAuth resource —
the auth is in the app's own code.

```bash
az group create -n rg-entra-inapp -l eastus2
az deployment group create -g rg-entra-inapp -f main.bicep -p main.bicepparam \
  -p clientSecret=$(az keyvault secret show --vault-name <kv> -n inapp-secret --query value -o tsv)
```

Then deploy the app code (`dotnet publish` → `az webapp deploy`).

## Cost

F1 is free. The Entra app registration, the OIDC sign-ins, and the Graph calls are
all Entra ID Free. The only thing that would add cost is moving to B1+ (e.g. to use
a managed identity / federated credential instead of the client secret, or for
Always On / custom domains / slots). See [`../../docs/COST.md`](../../docs/COST.md).

## Secrets

The client secret is never written to a file: the script prints it once (store it
in Key Vault), and `main.bicep` takes it as a `@secure` param injected at deploy,
exposed to the app as the `AzureAd__ClientSecret` app setting. For production, swap
that app setting for a Key Vault reference (commented in `main.bicep`).
