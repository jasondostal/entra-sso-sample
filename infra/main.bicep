// ─────────────────────────────────────────────────────────────────────────────
// App Service for the IN-APP auth variant (Microsoft.Identity.Web).
//
// Auth lives in the app's code, so the infra's only job is to run the app and
// hand it the AzureAd config as app settings. No special auth resource needed —
// contrast with easyauth.bicep, which moves sign-in to the platform layer.
//
// The Entra app registration is created out-of-band (see create-app-registration.sh)
// because Bicep cannot create app regs. Pass the resulting IDs in as params.
//
//   az deployment group create -g <rg> -f main.bicep -p main.bicepparam \
//     -p clientSecret=$(az keyvault secret show --vault-name <kv> -n entra-sample-secret --query value -o tsv)
// ─────────────────────────────────────────────────────────────────────────────

@description('Azure region.')
param location string = resourceGroup().location

@description('Base name for the App Service plan + web app (the web app name must be globally unique).')
param appName string

@description('Entra directory (tenant) ID — from create-app-registration.sh.')
param tenantId string

@description('Entra application (client) ID — from create-app-registration.sh.')
param clientId string

@description('Client secret. Inject from Key Vault at deploy time; never hardcode.')
@secure()
param clientSecret string

@description('App Service plan SKU.')
param sku string = 'B1'

resource plan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${appName}-plan'
  location: location
  sku: {
    name: sku
  }
  kind: 'linux'
  properties: {
    reserved: true // Linux
  }
}

resource site 'Microsoft.Web/sites@2023-12-01' = {
  name: appName
  location: location
  properties: {
    serverFarmId: plan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: 'DOTNETCORE|10.0'
      ftpsState: 'Disabled'
      minTlsVersion: '1.2'
      appSettings: [
        // These map to the "AzureAd" config section the app reads
        // (double-underscore = config nesting in .NET).
        { name: 'AzureAd__Instance', value: 'https://login.microsoftonline.com/' }
        { name: 'AzureAd__TenantId', value: tenantId }
        { name: 'AzureAd__ClientId', value: clientId }
        // In production prefer a Key Vault reference instead of the raw value:
        //   value: '@Microsoft.KeyVault(SecretUri=https://<kv>.vault.azure.net/secrets/entra-sample-secret/)'
        { name: 'AzureAd__ClientSecret', value: clientSecret }
        { name: 'AzureAd__CallbackPath', value: '/signin-oidc' }
        { name: 'AzureAd__SignedOutCallbackPath', value: '/signout-callback-oidc' }
      ]
    }
  }
}

@description('Register this exact redirect URI on the app registration.')
output redirectUri string = 'https://${site.properties.defaultHostName}/signin-oidc'

@description('Browse here after deploy.')
output appUrl string = 'https://${site.properties.defaultHostName}'
