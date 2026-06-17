// ─────────────────────────────────────────────────────────────────────────────
// App Service behind Entra SSO via App Service EasyAuth (authsettingsV2).
//
// One deploy gives you a sign-in-protected app with NO auth code: the App Service
// plan, the Linux .NET web app, the EasyAuth/Entra configuration, and the
// client-secret app setting that config references.
//
// The Entra app registration is created out-of-band (see create-app-registration.sh)
// because Bicep has no Microsoft.Graph provider. Pass the IDs + secret in:
//
//   az deployment group create -g <rg> -f main.bicep -p main.bicepparam \
//     -p clientSecret=$(az keyvault secret show --vault-name <kv> -n easyauth-secret --query value -o tsv)
// ─────────────────────────────────────────────────────────────────────────────

@description('Azure region.')
param location string = resourceGroup().location

@description('Globally-unique web app name.')
param appName string

@description('Entra directory (tenant) ID — from create-app-registration.sh.')
param tenantId string

@description('Entra application (client) ID — from create-app-registration.sh.')
param clientId string

@description('Client secret for EasyAuth. Inject from Key Vault at deploy; never hardcode.')
@secure()
param clientSecret string

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
      appSettings: [
        // EasyAuth reads the client secret from this app setting (referenced by
        // name in authsettingsV2 below — never inline). In prod use a KV reference:
        //   value: '@Microsoft.KeyVault(SecretUri=https://<kv>.vault.azure.net/secrets/easyauth-secret/)'
        {
          name: 'MICROSOFT_PROVIDER_AUTHENTICATION_SECRET'
          value: clientSecret
        }
      ]
    }
  }
}

// The auth layer. requireAuthentication + RedirectToLoginPage means every request
// is bounced to Entra sign-in before it ever reaches the app.
resource auth 'Microsoft.Web/sites/config@2023-12-01' = {
  parent: site
  name: 'authsettingsV2'
  properties: {
    platform: {
      enabled: true
    }
    globalValidation: {
      requireAuthentication: true
      unauthenticatedClientAction: 'RedirectToLoginPage'
      redirectToProvider: 'azureactivedirectory'
    }
    identityProviders: {
      azureActiveDirectory: {
        enabled: true
        registration: {
          openIdIssuer: 'https://login.microsoftonline.com/${tenantId}/v2.0'
          clientId: clientId
          clientSecretSettingName: 'MICROSOFT_PROVIDER_AUTHENTICATION_SECRET'
        }
        validation: {
          allowedAudiences: [
            'api://${clientId}'
          ]
        }
      }
    }
    login: {
      tokenStore: {
        enabled: true
      }
    }
  }
}

@description('Register this exact redirect URI on the app registration.')
output redirectUri string = 'https://${site.properties.defaultHostName}/.auth/login/aad/callback'

@description('Browse here after deploy.')
output appUrl string = 'https://${site.properties.defaultHostName}'
