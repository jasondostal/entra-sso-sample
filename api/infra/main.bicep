// ─────────────────────────────────────────────────────────────────────────────
// App Service for the protected Web API. The API only VALIDATES tokens, so the
// infra just runs it and hands it the AzureAd config — no client secret, no
// callback paths, no EasyAuth. Defaults to F1 (Free).
//
// The Entra app registration (with exposed scopes + app roles) is created
// out-of-band by create-app-registration.sh. Pass the IDs in:
//
//   az deployment group create -g <rg> -f main.bicep -p main.bicepparam
// ─────────────────────────────────────────────────────────────────────────────

@description('Azure region.')
param location string = resourceGroup().location

@description('Globally-unique web app name.')
param appName string

@description('Entra directory (tenant) ID — from create-app-registration.sh.')
param tenantId string

@description('Entra API application (client) ID — the token audience.')
param clientId string

@description('App Service plan SKU (F1 = free).')
param sku string = 'F1'

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
      alwaysOn: false // not supported on F1
      appSettings: [
        { name: 'AzureAd__Instance', value: 'https://login.microsoftonline.com/' }
        { name: 'AzureAd__TenantId', value: tenantId }
        { name: 'AzureAd__ClientId', value: clientId }
      ]
    }
  }
}

@description('Base URL of the API.')
output apiUrl string = 'https://${site.properties.defaultHostName}'
