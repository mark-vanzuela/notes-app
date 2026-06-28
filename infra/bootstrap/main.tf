# Bootstrap — run ONCE, with LOCAL state, before the main infra.
#
# It creates the two things the main Terraform depends on but can't create for
# itself (chicken-and-egg):
#   1. The Azure Storage account that holds the main config's REMOTE state.
#   2. The GitHub Actions OIDC identity (Entra app + federated credentials + role)
#      that the CI deploy job uses to log in to Azure with no stored secret.
#
# Apply it locally with your own `az login` session:
#   cd infra/bootstrap
#   terraform init
#   terraform apply
# Then copy the outputs into GitHub secrets + the main backend config (see README).

terraform {
  required_version = ">= 1.6"
  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~> 4.0" }
    azuread = { source = "hashicorp/azuread", version = "~> 3.0" }
    random  = { source = "hashicorp/random", version = "~> 3.6" }
  }
  # Bootstrap uses LOCAL state on purpose (it creates the remote backend itself).
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

provider "azuread" {}

data "azurerm_subscription" "current" {}
data "azuread_client_config" "current" {}

# --- Terraform remote-state storage ----------------------------------------
resource "azurerm_resource_group" "tfstate" {
  name     = var.tfstate_resource_group
  location = var.location
}

# Storage account names are GLOBALLY unique + 3-24 lowercase alphanumerics, so we
# append a short random suffix to the prefix.
resource "random_string" "sa_suffix" {
  length  = 6
  lower   = true
  upper   = false
  numeric = true
  special = false
}

resource "azurerm_storage_account" "tfstate" {
  name                     = "${var.tfstate_storage_prefix}${random_string.sa_suffix.result}"
  resource_group_name      = azurerm_resource_group.tfstate.name
  location                 = azurerm_resource_group.tfstate.location
  account_tier             = "Standard"
  account_replication_type = "LRS"
  min_tls_version          = "TLS1_2"

  blob_properties {
    versioning_enabled = true
  }
}

resource "azurerm_storage_container" "tfstate" {
  name                  = "tfstate"
  storage_account_name  = azurerm_storage_account.tfstate.name
  container_access_type = "private"
}

# --- GitHub Actions OIDC identity ------------------------------------------
resource "azuread_application" "github" {
  display_name = var.github_app_name
}

resource "azuread_service_principal" "github" {
  client_id = azuread_application.github.client_id
}

# Federated credential: GitHub Actions running on the `main` branch.
resource "azuread_application_federated_identity_credential" "main_branch" {
  application_id = azuread_application.github.id
  display_name   = "github-main"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.github_repo}:ref:refs/heads/main"
}

# Federated credential: GitHub Actions running in the `production` environment
# (used by the deploy job, which sets `environment: production`).
resource "azuread_application_federated_identity_credential" "prod_env" {
  application_id = azuread_application.github.id
  display_name   = "github-prod-env"
  audiences      = ["api://AzureADTokenExchange"]
  issuer         = "https://token.actions.githubusercontent.com"
  subject        = "repo:${var.github_repo}:environment:production"
}

# Let the CI identity manage resources in the subscription.
resource "azurerm_role_assignment" "github_contributor" {
  scope                = data.azurerm_subscription.current.id
  role_definition_name = "Contributor"
  principal_id         = azuread_service_principal.github.object_id
}
