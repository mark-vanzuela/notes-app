# Copy these into GitHub (Settings → Secrets and variables → Actions) and into the
# main config's backend init. See infra/bootstrap/README.md.

output "AZURE_CLIENT_ID" {
  description = "GitHub secret: the OIDC app's client id."
  value       = azuread_application.github.client_id
}

output "AZURE_TENANT_ID" {
  description = "GitHub secret: the Entra tenant id."
  value       = data.azuread_client_config.current.tenant_id
}

output "AZURE_SUBSCRIPTION_ID" {
  description = "GitHub secret: the subscription id."
  value       = data.azurerm_subscription.current.subscription_id
}

output "tfstate_resource_group" {
  description = "Backend config: resource group of the state storage."
  value       = azurerm_resource_group.tfstate.name
}

output "tfstate_storage_account" {
  description = "Backend config: name of the state storage account (random suffix)."
  value       = azurerm_storage_account.tfstate.name
}

output "tfstate_container" {
  description = "Backend config: blob container for state."
  value       = azurerm_storage_container.tfstate.name
}
