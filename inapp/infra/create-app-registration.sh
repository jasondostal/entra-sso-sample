#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# The "Entra side" for the IN-APP auth variant — created with az CLI, NOT Bicep.
#
# Bicep cannot create Entra app registrations (no Microsoft.Graph provider for
# ARM/Bicep). This script creates:
#   - the app registration, audience = this tenant only
#   - the WEB redirect URI .../signin-oidc  (Microsoft.Identity.Web's callback)
#   - delegated Microsoft Graph User.Read   (so the app can call GET /me)
#   - a service principal (enterprise app)
#   - a client secret (printed once — store it in Key Vault, never commit)
#
# Usage:
#   ./create-app-registration.sh "entra-inapp" "https://myapp.azurewebsites.net"
# Pass the app's HTTPS base URL so the right redirect URI is registered. For local
# dev, also register http://localhost:5000/signin-oidc (or pass that as the URL).
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

DISPLAY_NAME="${1:?usage: create-app-registration.sh <display-name> <app-base-url>}"
APP_BASE_URL="${2:?usage: create-app-registration.sh <display-name> <app-base-url>}"

# Microsoft.Identity.Web's default OIDC callback path.
REDIRECT="${APP_BASE_URL}/signin-oidc"

# Microsoft Graph well-known IDs (stable across all tenants).
GRAPH_APP_ID="00000003-0000-0000-c000-000000000000"
USER_READ_SCOPE_ID="e1fe6dd8-ba31-4d61-89e7-88639da4683d"  # delegated User.Read

echo ">> Creating app registration: ${DISPLAY_NAME}"
APP_ID=$(az ad app create \
  --display-name "${DISPLAY_NAME}" \
  --sign-in-audience AzureADMyOrg \
  --web-redirect-uris "${REDIRECT}" \
  --enable-id-token-issuance true \
  --required-resource-accesses "[{\"resourceAppId\":\"${GRAPH_APP_ID}\",\"resourceAccess\":[{\"id\":\"${USER_READ_SCOPE_ID}\",\"type\":\"Scope\"}]}]" \
  --query appId -o tsv)
echo "   clientId=${APP_ID}"

echo ">> Creating a service principal (enterprise app) for it"
az ad sp create --id "${APP_ID}" -o none

echo ">> Adding a client secret (valid 1 year)"
SECRET=$(az ad app credential reset \
  --id "${APP_ID}" \
  --display-name "inapp" \
  --years 1 \
  --query password -o tsv)

TENANT_ID=$(az account show --query tenantId -o tsv)

cat <<EOF

────────────────────────────────────────────────────────────────────────────
App registration ready. Feed these into main.bicep / main.bicepparam:

  tenantId  = ${TENANT_ID}
  clientId  = ${APP_ID}
  clientSecret (SENSITIVE — store in Key Vault, do NOT commit):
              ${SECRET}

Redirect URI registered:
  - ${REDIRECT}

Microsoft Graph User.Read (delegated) is requested. It's a user-consent scope,
so each user consents on first sign-in — no admin consent required.
────────────────────────────────────────────────────────────────────────────
EOF
