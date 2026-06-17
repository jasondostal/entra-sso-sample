using 'main.bicep'

// web app name must be globally unique
param appName = 'entra-sso-sample-dev'
param tenantId = 'REPLACE-WITH-DIRECTORY-TENANT-ID'
param clientId = 'REPLACE-WITH-APPLICATION-CLIENT-ID'

// Do NOT set clientSecret here. Pass it at deploy time from Key Vault:
//   -p clientSecret=$(az keyvault secret show --vault-name <kv> -n entra-sample-secret --query value -o tsv)
param clientSecret = ''
