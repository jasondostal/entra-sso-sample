#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# The "Entra side" for the protected Web API. A resource API exposes:
#   • delegated SCOPES   (oauth2PermissionScopes) — for user-delegated callers
#   • app ROLES          (appRoles, member type Application) — for daemon callers
#   • an Application ID URI (api://<clientId>) — the token audience
# No redirect URI and no client secret: the API only VALIDATES tokens, it doesn't
# sign anyone in (a secret would only be needed for on-behalf-of downstream calls).
#
# Usage:  ./create-app-registration.sh "entra-api-sample"
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

DISPLAY_NAME="${1:?usage: create-app-registration.sh <display-name>}"

uuid() { python3 -c 'import uuid;print(uuid.uuid4())'; }
SCOPE_READ=$(uuid); SCOPE_RW=$(uuid); ROLE_READ=$(uuid); ROLE_RW=$(uuid)

echo ">> Creating API app registration: ${DISPLAY_NAME}"
APP_ID=$(az ad app create \
  --display-name "${DISPLAY_NAME}" \
  --sign-in-audience AzureADMyOrg \
  --query appId -o tsv)
OBJ_ID=$(az ad app show --id "${APP_ID}" --query id -o tsv)
echo "   clientId=${APP_ID}"

echo ">> Setting Application ID URI to api://${APP_ID}"
az ad app update --id "${APP_ID}" --identifier-uris "api://${APP_ID}"

echo ">> Adding delegated scopes (Reports.Read, Reports.ReadWrite) + app roles"
BODY=$(cat <<JSON
{
  "api": {
    "oauth2PermissionScopes": [
      {
        "id": "${SCOPE_READ}", "isEnabled": true, "type": "User", "value": "Reports.Read",
        "adminConsentDisplayName": "Read reports", "adminConsentDescription": "Allows the app to read reports on behalf of the signed-in user.",
        "userConsentDisplayName": "Read your reports", "userConsentDescription": "Allows the app to read reports on your behalf."
      },
      {
        "id": "${SCOPE_RW}", "isEnabled": true, "type": "User", "value": "Reports.ReadWrite",
        "adminConsentDisplayName": "Read and write reports", "adminConsentDescription": "Allows the app to read and write reports on behalf of the signed-in user.",
        "userConsentDisplayName": "Read and write your reports", "userConsentDescription": "Allows the app to read and write reports on your behalf."
      }
    ]
  },
  "appRoles": [
    {
      "id": "${ROLE_READ}", "isEnabled": true, "allowedMemberTypes": ["Application"],
      "value": "Reports.Read", "displayName": "Read reports (app-only)", "description": "Daemon apps can read reports."
    },
    {
      "id": "${ROLE_RW}", "isEnabled": true, "allowedMemberTypes": ["Application"],
      "value": "Reports.ReadWrite", "displayName": "Read/write reports (app-only)", "description": "Daemon apps can read and write reports."
    }
  ]
}
JSON
)
az rest --method PATCH \
  --uri "https://graph.microsoft.com/v1.0/applications/${OBJ_ID}" \
  --headers "Content-Type=application/json" \
  --body "${BODY}"

echo ">> Creating a service principal (enterprise app)"
az ad sp create --id "${APP_ID}" -o none

TENANT_ID=$(az account show --query tenantId -o tsv)

cat <<EOF

────────────────────────────────────────────────────────────────────────────
API app registration ready. Feed these into main.bicep / main.bicepparam:

  tenantId = ${TENANT_ID}
  clientId = ${APP_ID}   (audience = api://${APP_ID})

Exposed delegated scopes:  Reports.Read, Reports.ReadWrite
Exposed app roles (daemon): Reports.Read, Reports.ReadWrite

Next:
  • Client apps request scope  api://${APP_ID}/Reports.Read  (delegated), or
  • Grant a daemon the Reports.* APP ROLE (Enterprise App > add a client app reg
    under "API permissions" > Application permissions, then admin-consent).
────────────────────────────────────────────────────────────────────────────
EOF
