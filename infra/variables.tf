variable "subscription_id" {
  description = "Azure subscription ID."
  type        = string
}

variable "location" {
  description = "Azure region."
  type        = string
  default     = "southeastasia"
}

variable "resource_group_name" {
  description = "Resource group for the app."
  type        = string
  default     = "notes-app-rg"
}

variable "log_analytics_name" {
  description = "Log Analytics workspace name."
  type        = string
  default     = "notes-app-logs"
}

variable "log_daily_quota_gb" {
  description = "Daily ingestion cap (GB) for Log Analytics — the cost guardrail."
  type        = number
  default     = 0.5
}

variable "aca_env_name" {
  description = "Container Apps environment name."
  type        = string
  default     = "notes-app-env"
}

variable "api_app_name" {
  description = "Container App name for the API."
  type        = string
  default     = "notes-app-api"
}

variable "web_app_name" {
  description = "Container App name for the web frontend."
  type        = string
  default     = "notes-app-web"
}

variable "api_image" {
  description = "API image repository (no tag)."
  type        = string
  default     = "ghcr.io/mark-vanzuela/notes-app-api"
}

variable "web_image" {
  description = "Web image repository (no tag)."
  type        = string
  default     = "ghcr.io/mark-vanzuela/notes-app-web"
}

variable "image_tag" {
  description = "Image tag to deploy. CI updates images out-of-band via `az containerapp update`; Terraform pins the initial tag."
  type        = string
  default     = "latest"
}

variable "max_replicas" {
  description = "Max replicas per app — bounds worst-case compute spend."
  type        = number
  default     = 2
}

# --- Secrets / sensitive (set in terraform.tfvars, which is git-ignored) ----
variable "neon_pooled_connection_string" {
  description = "Neon POOLED Npgsql connection string for app runtime (SSL Mode=Require)."
  type        = string
  sensitive   = true
}

variable "jwt_key" {
  description = "Production JWT signing key (>= 32 chars)."
  type        = string
  sensitive   = true
}

variable "google_client_id" {
  description = "Google OAuth Web client id (matches the frontend's VITE_GOOGLE_CLIENT_ID)."
  type        = string
}

# --- Budget ----------------------------------------------------------------
variable "budget_amount" {
  description = "Monthly budget alert threshold in the subscription currency (a tripwire, not a charge)."
  type        = number
  default     = 1
}

variable "budget_alert_email" {
  description = "Email to notify on budget thresholds."
  type        = string
}

variable "budget_start_date" {
  description = "Budget start — must be the first of a month, UTC (e.g. 2026-07-01T00:00:00Z)."
  type        = string
  default     = "2026-07-01T00:00:00Z"
}
