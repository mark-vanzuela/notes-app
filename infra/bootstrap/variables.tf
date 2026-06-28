variable "subscription_id" {
  description = "Azure subscription ID (from `az account show --query id -o tsv`)."
  type        = string
}

variable "location" {
  description = "Azure region for the tfstate resource group."
  type        = string
  default     = "southeastasia"
}

variable "tfstate_resource_group" {
  description = "Resource group that holds the Terraform remote-state storage."
  type        = string
  default     = "notes-app-tfstate-rg"
}

variable "tfstate_storage_prefix" {
  description = "Prefix for the globally-unique state storage account name (a random suffix is appended). Lowercase alphanumerics only."
  type        = string
  default     = "notesapptf"
}

variable "github_app_name" {
  description = "Display name for the Entra app registration used by GitHub Actions OIDC."
  type        = string
  default     = "notes-app-github-oidc"
}

variable "github_repo" {
  description = "GitHub repo in 'owner/name' form, for the OIDC federated-credential subjects."
  type        = string
  default     = "mark-vanzuela/notes-app"
}
