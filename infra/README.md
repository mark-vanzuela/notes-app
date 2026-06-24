# Infra — Azure infrastructure-as-code

Infrastructure-as-code for the single Azure (production) environment.

> **Status:** deferred. To be defined in the infra planning round.
>
> Open decision: Azure compute target — **Container Apps** vs **App Service for
> Containers** — which drives the IaC tool choice (Bicep or Terraform) and the
> deploy steps in `.github/workflows/`.
>
> Scope when built: container registry, compute for web + api, secrets/connection
> string wiring (Neon), and any networking/observability.
