# Infra — Azure (Terraform → Container Apps)

Infrastructure-as-code for the single Azure (production) environment: two **Container Apps**
(api + web) pulling public ghcr images, scale-to-zero, **Log Analytics** (with a daily cap),
and a **budget alert**. Remote Terraform state in Azure Storage; CI deploys via **OIDC**.

## Prerequisites
- `az login` (active subscription) and **Terraform** installed.
- **Neon** project created → a **pooled** connection string (app runtime) and a **direct**
  one (migrations). See repo `CLAUDE.md` → Neon.

## Run order

**1. Bootstrap (once):** creates the state storage + GitHub OIDC identity.
```bash
cd infra/bootstrap
cp terraform.tfvars.example terraform.tfvars   # set subscription_id
terraform init && terraform apply
```
Then set GitHub repo **secrets** from the outputs: `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`,
`AZURE_SUBSCRIPTION_ID`. Note the `tfstate_storage_account` output for the next step.

**2. Main infra:**
```bash
cd ..                                          # infra/
cp terraform.tfvars.example terraform.tfvars   # set subscription_id, secrets, budget email
terraform init -backend-config="storage_account_name=<tfstate_storage_account>"
terraform apply
```
Read the outputs: `web_url`, `api_url`, `api_fqdn`, `web_fqdn`, `resource_group_name`.

**3. Wire GitHub + Google (after apply):**
- Repo **secrets**: `DATABASE_CONNECTION_STRING` = Neon **direct** string (for migrations).
- Repo **variables**: `VITE_API_BASE_URL` = `<api_url>`, `AZURE_RESOURCE_GROUP`,
  `ACA_API_NAME` (`notes-app-api`), `ACA_WEB_NAME` (`notes-app-web`).
- Google Cloud Console → OAuth client → add `<web_url>` to **Authorized JavaScript origins**.

**4. First deploy:** the web image bakes `VITE_API_BASE_URL` at build, so after setting that
repo variable, re-run the **frontend** workflow (publish + deploy) so the web image points at
the real API. Migrations run in the **api** deploy job (or via the `db-migrate` workflow)
against the Neon **direct** endpoint.

## Files
- `bootstrap/` — one-time, local state (see its README).
- `backend.tf` — provider + remote state backend.
- `main.tf` — RG, Log Analytics (+daily cap), ACA env, api + web apps, budget.
- `variables.tf` / `outputs.tf` / `terraform.tfvars.example`.

> State files and `terraform.tfvars` are git-ignored. The `.terraform.lock.hcl` lock files
> ARE committed.
