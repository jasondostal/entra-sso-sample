#!/usr/bin/env bash
# ─────────────────────────────────────────────────────────────────────────────
# The "Entra side" — created with az CLI, NOT Bicep.
#
# Bicep cannot create Entra app registrations: there is no Microsoft.Graph
# resource provider for ARM/Bicep. App regs are made via az CLI, the portal, or
# Terraform's azuread provider. So this script is the IaC for the Entra half;
# main.bicep consumes the IDs it prints.
#
# Usage:
#   ./create-app-registration.sh "myapp-web" "https://myapp.azurewebsites.net"
# Pass the app's HTTPS base URL so the right redirect URIs get registered. Add
# https://localhost:5001 too for local dev (edit REDIRECTS below).
# ─────────────────────────────────────────────────────────────────────────────
set -euo pipefail

DISPLAY_NAME="${1:?usage: create-app-registration.sh <display-name> <app-base-url>}"
APP_BASE_URL="${2:?usage: create-app-registration.sh <display-name> <app-base-url>}"

# Auth style decides which redirect path Entra needs:
#   in-app (Microsoft.Identity.Web): <base>/signin-oidc
#   EasyAuth (App Service):          <base>/.auth/login/aad/callback
# Register both so either bicep variant works.
REDIRECTS=(
  "${APP_BASE_URL}/signin-oidc"
  "${APP_BASE_URL}/.auth/login/aad/callback"
  "https://localhost:5001/signin-oidc"
)

echo ">> Creating app registration: ${DISPLAY_NAME}"
APP_ID=$(az ad app create \
  --display-name "${DISPLAY_NAME}" \
  --sign-in-audience AzureADMyOrg \
  --web-redirect-uris "${REDIRECTS[@]}" \
  --enable-id-token-issuance true \
  --query appId -o tsv)

echo ">> Creating a service principal (enterprise app) for it"
az ad sp create --id "${APP_ID}" -o none

echo ">> Adding a client secret (valid 1 year)"
SECRET=$(az ad app credential reset \
  --id "${APP_ID}" \
  --display-name "bicep-sample" \
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

Redirect URIs registered:
$(printf '  - %s\n' "${REDIRECTS[@]}")
────────────────────────────────────────────────────────────────────────────
EOF
