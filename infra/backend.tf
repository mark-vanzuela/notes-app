terraform {
  required_version = ">= 1.6"

  required_providers {
    azurerm = { source = "hashicorp/azurerm", version = "~> 4.0" }
  }

  # Remote state in the Azure Storage account created by infra/bootstrap.
  # storage_account_name is supplied at init (it carries a random suffix):
  #   terraform init -backend-config="storage_account_name=<bootstrap output>"
  backend "azurerm" {
    resource_group_name = "notes-app-tfstate-rg"
    container_name      = "tfstate"
    key                 = "notes-app.tfstate"
  }
}

provider "azurerm" {
  features {}
  subscription_id = var.subscription_id
}

data "azurerm_subscription" "current" {}
