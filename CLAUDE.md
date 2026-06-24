# CLAUDE.md — Notes App

Guidance for Claude Code when working in this repo. A full-stack notes app built as a
**portfolio / skills demo** (full-stack + DevOps). Built incrementally — each app gets
its own planning round.

## Architecture

```
React SPA (Vite+TS)  ──HTTP/JSON──►  ASP.NET Core Web API (.NET 8)  ──Npgsql/EF Core──►  Postgres
```

- **Monorepo**, but frontend and API **deploy independently** (path-filtered CI). A
  monorepo does NOT force coupled deployment.

## Layout

| Path                 | Purpose                                                   |
| -------------------- | --------------------------------------------------------- |
| `frontend/`          | React + Vite + TypeScript SPA                             |
| `api/`               | ASP.NET Core (.NET 8) solution + EF Core migrations       |
| `infra/`             | Azure IaC (deferred)                                      |
| `.github/workflows/` | CI/CD, path-filtered per app                              |
| `docker-compose.yml` | Local dev: web + api + postgres                           |

- EF Core **code-first migrations live in `api/src/NotesApp.Infrastructure/Migrations/`**
  (the DbContext's assembly — the idiomatic EF location). The C# solution owns the
  schema — there is no separate SQL/db repo.

## Tooling (decided)

- Frontend: React + Vite + TypeScript
- API: .NET 8 (LTS), controller-based Web API
- Data: EF Core 8 + Npgsql, code-first migrations
- DB: Postgres 16 (local via Compose) / **Neon free tier** (prod)
- Containers: multi-stage Dockerfiles per app
- Registry: **GitHub Container Registry (ghcr.io)** — free for public images; CI pushes,
  ACA pulls directly (no ACR)
- IaC: **Terraform** (`azurerm` provider), remote state in an **Azure Storage backend**;
  CI auth via service principal / **OIDC** (no long-lived secret)
- Compute (Azure): **Azure Container Apps (ACA)** for BOTH api and frontend, Consumption
  plan, **`minReplicas = 0`** (scale-to-zero → idle ≈ $0)

## Environments & deployment (decided)

- **dev = local Docker Compose** (web + api + postgres). No hosted dev env in Azure.
- **`main` = production = the single Azure environment.** No `prod` branch.
- Flow: feature branch → **PR to `main`** runs CI **validation only** (build + test +
  docker build, NO deploy) → **merge to `main`** triggers **path-filtered deploy** to
  Azure (frontend changed → deploy web; api changed → deploy api; both → both).
- Each workflow has `workflow_dispatch` for manual/force deploys + initial seed.
- Deploy job is guarded by `if: github.event_name != 'pull_request'`.
- Optional later: GitHub Environment protection rule (manual approval) before deploy.

## API (built)

Clean architecture mirroring the user's `dotnet-angular-react/CustomerApi`:
`NotesApp.Api` / `.Application` / `.Domain` / `.Infrastructure` + `NotesApp.UnitTests`.
CQRS via MediatR (vertical-slice feature folders), FluentValidation + a
`ValidationBehavior` pipeline, ProblemDetails `ExceptionHandlingMiddleware`, thin
controllers → `ISender`, manual entity→DTO mapping, Swagger.

- **Persistence:** EF Core 8 + Npgsql. `NotesDbContext` (+ `NotesDbContextFactory` for
  design-time tooling), `EfNoteRepository`/`EfUserRepository` (Scoped, save immediately).
  Soft-delete via global query filter `HasQueryFilter(n => !n.IsDeleted)`.
- **Entities:** `Note` (Id, UserId, Title, Content, IsDeleted, audit) and `User`
  (Id, GoogleSubjectId unique, Email, Name, PictureUrl, audit). Private setters + static
  factories.
- **Endpoints:** `POST /api/auth/google` (anonymous); `[Authorize]` CRUD at `api/notes`.
- **Dev migrate-on-startup** in Development only (`db.Database.Migrate()` in Program.cs);
  prod applies migrations via the deploy pipeline (see Neon section).
- **Run:** `docker compose up --build db api`; Swagger at `http://localhost:8080/swagger`.
  Build/test from `api/`: `dotnet build` / `dotnet test`.

## Auth (Google sign-in → app JWT)

- Frontend (later round) gets a Google **ID token** via Google Identity Services →
  `POST /api/auth/google { idToken }` → API **verifies** it (`Google.Apis.Auth`, audience
  = our Google Client ID) → **upserts a `User`** (first sight of a Google `sub` = sign-up)
  → returns the app's **own JWT** (HMAC-SHA256, claims `sub`=User.Id/email/name).
- Subsequent requests send `Authorization: Bearer <app-jwt>`; JwtBearer validates it.
  `ICurrentUser`/`CurrentUser` reads the user id from the `sub`/NameIdentifier claim;
  controllers set note ownership from it — **never** from the request body.
- **Per-user notes:** all repo methods are scoped by `userId`; another user's note → 404.
- **Config / secrets** (`.env.example` + compose env mirror these):
  `Authentication__Google__ClientId`, `Jwt__Issuer`, `Jwt__Audience`, `Jwt__Key` (secret),
  `Jwt__ExpiryMinutes`. Dev placeholders in `appsettings.Development.json`; prod via
  env/Key Vault. Google Client ID is created in Cloud Console (Web app, JS origin
  `http://localhost:5173`) — only needed for real token verification.
- **Deferred:** refresh tokens / logout-revocation (app JWT is short-lived).

## Connection string convention

The API **always** reads `ConnectionStrings__Default` from its environment. Only the
*value* changes per environment — code never changes:

- **Local:** `Host=db;Port=5432;Database=notesdb;Username=notes;Password=...` (the
  Compose `db` service). See `.env.example`.
- **Prod:** the Neon connection string, injected as an Azure app setting / Key Vault
  ref. Never committed.

## Neon (prod Postgres) — how it works

1. Create a Neon project → get the connection string. Neon shows a `postgresql://…`
   URL; reformat to the Npgsql key=value form. **SSL is mandatory** (`SSL Mode=Require`).
2. Store secrets — never in code/committed `.env`:
   - GitHub Actions repo secret (e.g. `NEON_CONNECTION_STRING`) for the deploy.
   - Azure env var `ConnectionStrings__Default` (prefer Key Vault) for the running API.
3. **Migrations** applied via the **`db-migrate` GitHub workflow** (manual
   `workflow_dispatch`): runs `dotnet ef database update` against the target DB using the
   `DATABASE_CONNECTION_STRING` secret, and uploads the idempotent SQL as an audit
   artifact. Locally: `./scripts/db-migrate.sh [--connection <conn>] [--target <name|0>]`.
   This is preferred over `Database.Migrate()` on startup (avoids multi-replica race +
   implicit schema-on-boot). **Use Neon's DIRECT (non-pooled) endpoint** for migrations.
4. API connects to Neon via the same `ConnectionStrings__Default` var — identical code.

**Neon gotchas:**
- **Pooled vs direct endpoint:** use the **direct** endpoint (no `-pooler`) for
  **migrations** (DDL/pooling issues); use the **pooled** endpoint for **app runtime**.
  May mean two secrets.
- **Auto-suspend (free tier):** scales to zero after ~5 min idle → first request after
  idle has a cold-start delay. Fine for a demo.

## Status & next rounds

**Done:** repo structure; **API round** (notes CRUD + Google auth + per-user ownership +
EF Core/Postgres + `InitialCreate` migration + unit tests + Dockerfile + compose `api`
service). Build + tests pass; container e2e (`docker compose up --build db api`) not yet
run locally (Docker Desktop needs reinstall).

**Deferred:** frontend app; CI job bodies; Azure IaC (Terraform); Neon provisioning + prod
migration pipeline.

Planned rounds (each planned separately before building): **frontend** → **infra/CI bodies**.

## Estimated cost (demo traffic: owner + occasional reviewer)

| Component | Est. / month | Notes |
| --- | --- | --- |
| ACA compute (api + web, scale-to-zero) | $0 | Idle = 0 replicas; stays inside ACA free grant |
| Neon Postgres | $0 | Free tier, auto-suspends |
| Log Analytics | ~$0 | ~5 GB/mo free; trivial traffic |
| Terraform state (Azure Storage) | ~$0 | Few KB blob |
| Egress | ~$0 | First 100 GB/mo free |
| Container Registry (**ghcr.io**) | $0 | Free for public images; ACA pulls from it |

**Total ≈ $0/month** at demo traffic.

Registry decision: **GitHub Container Registry (ghcr.io)** — free for public images,
already on GitHub, ACA pulls directly. (ACR Basic ~$5/mo flat was the alternative; not
used.)

## Cost guardrails (DDoS / runaway-billing protection)

Azure pay-as-you-go has **no native hard spending cap**. Protection is layered:

1. **Cap `maxReplicas` low** (e.g. 1–3) + small CPU/memory per ACA app. This is the
   single most important control: it bounds the *maximum possible* compute spend
   regardless of traffic — even under a flood, only N replicas can ever run.
2. **Log Analytics daily cap.** A traffic flood generates huge log volume → cost. Set a
   **daily ingestion cap** on the workspace (and keep logging minimal).
3. **Azure Cost Management budget + alerts** to get notified early (alerts only notify;
   they don't stop spend by themselves).
4. **Optional kill switch:** budget alert → Action Group → Automation/Logic App that
   stops/deletes resources for a true auto-cap.
5. **App-level rate limiting** in the API (and/or nginx in the frontend container).
   ACA ingress has no built-in WAF; a full WAF (Azure Front Door) costs money, so skip
   it for the demo and rely on `maxReplicas` + rate limiting.
6. Consider deploying under an Azure account type that has a **hard spending limit**
   (free trial / student) if absolute-zero-billing certainty is required.

## Open decisions

- (Resolved) IaC = Terraform; compute = Azure Container Apps. See above.
- Frontend hosting: ACA container (chosen, for the "all containers" story) vs Azure
  Static Web Apps free tier (cheaper but not a container) — revisit only if cost matters
  more than the narrative.

## Conventions

- `.env` is git-ignored; only `.env.example` is committed. Never commit secrets.
- Don't add app code, Dockerfile bodies, or CI job bodies ahead of their planning round
  without checking with the user.
