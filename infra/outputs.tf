# After apply, use these to wire GitHub repo variables + Google OAuth origins.

output "api_fqdn" {
  description = "API public hostname. Set repo var VITE_API_BASE_URL = https://<this>/api"
  value       = azurerm_container_app.api.ingress[0].fqdn
}

output "web_fqdn" {
  description = "Web public hostname. Add https://<this> to Google OAuth authorized origins."
  value       = azurerm_container_app.web.ingress[0].fqdn
}

output "api_url" {
  description = "Convenience: full API base URL."
  value       = "https://${azurerm_container_app.api.ingress[0].fqdn}/api"
}

output "web_url" {
  description = "Convenience: the live app URL."
  value       = "https://${azurerm_container_app.web.ingress[0].fqdn}"
}

output "resource_group_name" {
  description = "For the CI deploy job (AZURE_RESOURCE_GROUP variable)."
  value       = azurerm_resource_group.main.name
}
