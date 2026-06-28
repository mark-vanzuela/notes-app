# infra/bootstrap

One-time setup that the main config depends on. Uses **local state** (it creates the
remote state backend itself). Run it with your own `az login` session.

## What it creates
- A resource group + **storage account + container** for the main config's remote Terraform state.
- A **GitHub Actions OIDC identity** (Entra app registration + service principal +
  federated credentials for `main` branch and the `production` environment) and a
  **Contributor** role assignment, so CI deploys to Azure with no stored secret.

## Run
```bash
cd infra/bootstrap
cp terraform.tfvars.example terraform.tfvars   # then set subscription_id
terraform init
terraform validate
terraform apply
```

## After apply — wire the outputs
Set these as GitHub repo **secrets** (Settings → Secrets and variables → Actions → Secrets):
- `AZURE_CLIENT_ID`, `AZURE_TENANT_ID`, `AZURE_SUBSCRIPTION_ID`

Use the storage outputs to init the **main** config's backend:
```bash
cd ../              # infra/
terraform init -backend-config="storage_account_name=<tfstate_storage_account output>"
```
(`resource_group_name`, `container_name`, and `key` are already set in `backend.tf`.)
