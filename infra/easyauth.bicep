// ─────────────────────────────────────────────────────────────────────────────
// App Service EasyAuth variant (NO app code) — the platform-layer alternative
// to main.bicep's in-app auth. App Service intercepts requests, runs the Entra
// sign-in itself, and injects identity headers; the app stays auth-unaware.
//
// Use EITHER this OR the in-app app settings from main.bicep, not both.
//
// Apply against an existing web app:
//   az deployment group create -g <rg> -f easyauth.bicep \
//     -p siteName=<app> tenantId=<tid> clientId=<cid>
//
// The client secret is referenced by name, not value: set the app setting
// MICROSOFT_PROVIDER_AUTHENTICATION_SECRET on the site first (from Key Vault),
// e.g. az webapp config appsettings set -g <rg> -n <app> \
//        --settings MICROSOFT_PROVIDER_AUTHENTICATION_SECRET=<secret>
//
// Redirect URI to register on the app reg: https://<app>/.auth/login/aad/callback
// ─────────────────────────────────────────────────────────────────────────────

@description('Name of the existing App Service web app to protect.')
param siteName string

@description('Entra directory (tenant) ID.')
param tenantId string

@description('Entra application (client) ID.')
param clientId string

resource site 'Microsoft.Web/sites@2023-12-01' existing = {
  name: siteName
}

resource authConfig 'Microsoft.Web/sites/config@2023-12-01' = {
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
          // points at the app setting holding the secret — not the secret itself
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
