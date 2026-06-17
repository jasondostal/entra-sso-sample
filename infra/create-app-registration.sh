#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# The "Entra side" for App Service EasyAuth — created with az CLI, NOT Bicep.
#
# Bicep cannot create Entra app registrations: there is no Microsoft.Graph
# resource provider for ARM/Bicep. App regs are made via az CLI, the portal, or
# Terraform's azuread provider. main.bicep consumes the IDs this prints.
#
# Usage:
#   ./create-app-registration.sh "myapp-easyauth" "https://myapp.azurewebsites.net"
# Pass the app's HTTPS base URL so the right EasyAuth redirect URI is registered.
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

DISPLAY_NAME="${1:?usage: create-app-registration.sh <display-name> <app-base-url>}"
APP_BASE_URL="${2:?usage: create-app-registration.sh <display-name> <app-base-url>}"

# EasyAuth's redirect URI is always /.auth/login/aad/callback on the app host.
REDIRECT="${APP_BASE_URL}/.auth/login/aad/callback"

echo ">> Creating app registration: ${DISPLAY_NAME}"
APP_ID=$(az ad app create \
  --display-name "${DISPLAY_NAME}" \
  --sign-in-audience AzureADMyOrg \
  --web-redirect-uris "${REDIRECT}" \
  --query appId -o tsv)
echo "   clientId=${APP_ID}"

echo ">> Creating a service principal (enterprise app) for it"
az ad sp create --id "${APP_ID}" -o none

echo ">> Adding a client secret (valid 1 year)"
SECRET=$(az ad app credential reset \
  --id "${APP_ID}" \
  --display-name "easyauth" \
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
────────────────────────────────────────────────────────────────────────────
EOF
