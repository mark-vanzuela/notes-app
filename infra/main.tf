# Main infrastructure for the single Azure (production) environment.
# Provider + backend live in backend.tf. Apply locally after bootstrap:
#   terraform init -backend-config="storage_account_name=<bootstrap output>"
#   terraform apply

# --- Resource group --------------------------------------------------------
resource "azurerm_resource_group" "main" {
  name     = var.resource_group_name
  location = var.location
}

# --- Log Analytics (container logs land here) ------------------------------
# daily_quota_gb is the COST GUARDRAIL: ingestion above this stops for the day,
# bounding spend even under a traffic/log flood.
resource "azurerm_log_analytics_workspace" "main" {
  name                = var.log_analytics_name
  resource_group_name = azurerm_resource_group.main.name
  location            = azurerm_resource_group.main.location
  sku                 = "PerGB2018"
  retention_in_days   = 30
  daily_quota_gb      = var.log_daily_quota_gb
}

# --- Container Apps environment --------------------------------------------
resource "azurerm_container_app_environment" "main" {
  name                       = var.aca_env_name
  resource_group_name        = azurerm_resource_group.main.name
  location                   = azurerm_resource_group.main.location
  log_analytics_workspace_id = azurerm_log_analytics_workspace.main.id
}

# --- API container app -----------------------------------------------------
resource "azurerm_container_app" "api" {
  name                         = var.api_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  # App secrets (ACA-native; referenced by env vars below).
  secret {
    name  = "connectionstrings-default"
    value = var.neon_pooled_connection_string
  }
  secret {
    name  = "jwt-key"
    value = var.jwt_key
  }

  ingress {
    external_enabled = true
    target_port      = 8080
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 0 # scale-to-zero → idle ≈ $0
    max_replicas = var.max_replicas

    container {
      name   = "api"
      image  = "${var.api_image}:${var.image_tag}"
      cpu    = 0.25
      memory = "0.5Gi"

      env {
        name  = "ASPNETCORE_ENVIRONMENT"
        value = "Production"
      }
      env {
        name        = "ConnectionStrings__Default"
        secret_name = "connectionstrings-default"
      }
      env {
        name        = "Jwt__Key"
        secret_name = "jwt-key"
      }
      env {
        name  = "Jwt__Issuer"
        value = "notes-app"
      }
      env {
        name  = "Jwt__Audience"
        value = "notes-app"
      }
      env {
        name  = "Authentication__Google__ClientId"
        value = var.google_client_id
      }
      # Allow the deployed web app's origin through CORS.
      env {
        name  = "Cors__AllowedOrigins"
        value = "https://${azurerm_container_app.web.ingress[0].fqdn}"
      }

      liveness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/health/live"
      }
      readiness_probe {
        transport = "HTTP"
        port      = 8080
        path      = "/health/ready"
      }
    }
  }
}

# --- Web (nginx) container app ---------------------------------------------
resource "azurerm_container_app" "web" {
  name                         = var.web_app_name
  container_app_environment_id = azurerm_container_app_environment.main.id
  resource_group_name          = azurerm_resource_group.main.name
  revision_mode                = "Single"

  ingress {
    external_enabled = true
    target_port      = 80
    transport        = "auto"

    traffic_weight {
      latest_revision = true
      percentage      = 100
    }
  }

  template {
    min_replicas = 0
    max_replicas = var.max_replicas

    container {
      name   = "web"
      image  = "${var.web_image}:${var.image_tag}"
      cpu    = 0.25
      memory = "0.5Gi"

      liveness_probe {
        transport = "HTTP"
        port      = 80
        path      = "/"
      }
    }
  }
}

# --- Budget alert (free; a tripwire, not a charge) -------------------------
resource "azurerm_consumption_budget_subscription" "main" {
  name            = "notes-app-budget"
  subscription_id = data.azurerm_subscription.current.id

  amount     = var.budget_amount
  time_grain = "Monthly"

  time_period {
    start_date = var.budget_start_date
  }

  dynamic "notification" {
    for_each = [50, 90, 100]
    content {
      enabled        = true
      threshold      = notification.value
      operator       = "GreaterThanOrEqualTo"
      threshold_type = "Actual"
      contact_emails = [var.budget_alert_email]
    }
  }
}
