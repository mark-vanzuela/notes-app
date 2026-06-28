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

- **dev = local Docker Compose** (web + api + postgres + pgadmin). No hosted dev env in Azure.
- **Branches: `dev` (integration) + `main` (production).** Feature branches → PR into
  `dev`; PR **`dev`→`main`** to release. Local-dev environment = Compose, not a branch.
- **CI/CD (built):** two path-aware workflows, `.github/workflows/api.yml` &
  `frontend.yml`:
  - **`validate`** ("API CI" / "Frontend CI") runs on **every PR** to `dev`/`main`
    (NOT path-filtered, so the required checks always report): api = dotnet build+test;
    frontend = npm ci + lint + test + build.
  - **`publish`** runs only on **push to `main`** (path-filtered) — builds & pushes images
    to **ghcr** (`ghcr.io/mark-vanzuela/notes-app-{api,web}`, tags `latest` + `sha-<sha>`)
    via the built-in `GITHUB_TOKEN` (`packages: write`). Images are **public**.
  - `concurrency` cancels superseded runs; `workflow_dispatch` for manual runs.
- **Branch protection on `main`** (set via `gh api`): require a PR (no direct pushes),
  require status checks **"API CI" + "Frontend CI"** to pass + branch up-to-date, block
  force-push/deletion. **No required approval** (solo dev can't self-approve) — the user
  reviews and merges. `enforce_admins: false` so the owner can merge their own PRs.
  → **Don't push to `main` directly; land work on `dev` and open a PR.**
- **Frontend image is build-time-env**: `publish` passes `VITE_API_BASE_URL` +
  `VITE_GOOGLE_CLIENT_ID` from GitHub Actions **repo variables** (`vars.*`, public). Set
  `VITE_GOOGLE_CLIENT_ID` now; set `VITE_API_BASE_URL` to the real API URL in the infra round.
- **One-time manual:** flip the two ghcr packages to **Public** after first publish.
- **Deploy-to-Azure (built — infra round):** each workflow's `deploy` job (gated
  `if: … && vars.DEPLOY_ENABLED == 'true'`, `environment: production`) logs in via **OIDC**
  (`AZURE_CLIENT_ID/TENANT_ID/SUBSCRIPTION_ID` secrets) and runs
  `az containerapp update --image …:sha-<full-sha>` (immutable tag → ACA rolls a new
  revision). The **api** deploy first runs EF Core migrations against Neon's **direct**
  endpoint (`DATABASE_CONNECTION_STRING` secret). Set `DEPLOY_ENABLED=true` only after the
  first `terraform apply`. Images are tagged `sha-<full-sha>` (publish uses `format=long`).

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

## Frontend (built)

React 19 + Vite + TypeScript SPA in `frontend/`, mirroring the user's
`dotnet-angular-react/react-app` conventions: **Redux Toolkit** (slices + async thunks +
selectors + typed hooks), **CSS modules** + global utilities in `index.css`, a typed
`fetch` wrapper (`api/client.ts`) + per-resource API modules, feature folders,
react-router v7. Heavily commented; see `frontend/FLOW.md` for the learning walkthrough.

- **Auth**: `@react-oauth/google` `<GoogleLogin>` → Google ID token →
  `loginWithGoogle` thunk → `POST /api/auth/google` → app JWT + user, stored in Redux +
  `localStorage` (survives refresh). `api/client.ts` attaches `Bearer` token and triggers
  `logout` on 401. `RequireAuth` guards note routes; `/login` otherwise.
- **Notes UI**: responsive **card grid** (`NotesList`) + create/edit `NoteForm`.
- **Testing**: **Vitest + React Testing Library** (jsdom). `npm run test:run`. Slice tests
  (mock the API module via `vi.mock`) + a `NotesList` component test via a
  `renderWithProviders` helper (`src/test/test-utils.tsx`).
- **Config (build-time, baked by Vite)**: `VITE_API_BASE_URL`, `VITE_GOOGLE_CLIENT_ID`
  (`frontend/.env`, gitignored). **Real sign-in needs the Google Client ID set in BOTH
  the frontend (`VITE_GOOGLE_CLIENT_ID`) and the API (`Authentication:Google:ClientId`) —
  they must match**, and the OAuth client's authorized JS origin must include
  `http://localhost:5173`.
- **Container**: multi-stage `frontend/Dockerfile` (node build → nginx, SPA fallback in
  `nginx.conf`); `web` service in compose (build args pass the `VITE_*` values).
- **Run**: `cd frontend && npm install && npm run dev` (→ `:5173`), or whole stack via
  `docker compose up --build`.

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

## Infra (Azure, Terraform — built)

IaC lives in `infra/`. Single prod environment on **Azure Container Apps**.
- **`infra/bootstrap/`** (run once, **local state**): creates the Terraform remote-state
  storage (RG `notes-app-tfstate-rg` + storage account w/ random suffix + `tfstate`
  container) and the **GitHub OIDC identity** (Entra app + SP + federated creds for
  `ref:refs/heads/main` and `environment:production` + Contributor role). Outputs →
  `AZURE_*` GitHub secrets + the state storage-account name.
- **`infra/`** (remote state via `-backend-config="storage_account_name=…"`): RG
  `notes-app-rg` (region `southeastasia`), **Log Analytics** with `daily_quota_gb` cap,
  ACA environment, **api** + **web** Container Apps (public ghcr images, `min_replicas=0`,
  `max_replicas=2`, ACA secrets for Neon-pooled + JWT key, health probes →
  `/health/live` + `/health/ready`), and a **budget alert** (default $1 tripwire).
- **Secrets**: app secrets are **ACA-native** (`secret {}` blocks), values from a
  git-ignored `terraform.tfvars`. Terraform is applied **locally** (not in CI); CI only
  rolls the image. State files + `*.tfvars` git-ignored; `.terraform.lock.hcl` committed.
- **Run order + the build-time-env wrinkle** (web bakes `VITE_API_BASE_URL` at build, known
  only after apply): see `infra/README.md`.

## Status & next rounds

**Done:** repo structure; **API round** (notes CRUD + Google auth + per-user ownership +
EF Core/Postgres + `InitialCreate` migration + unit tests + Dockerfile + compose `api`
service) — verified end-to-end via `docker compose up`; pgAdmin added at `:5050`.
**Frontend round** (React+Vite+TS SPA, Google sign-in, notes card-grid CRUD, Redux
Toolkit, Vitest+RTL tests, nginx Dockerfile + compose `web`) — verified end-to-end in
containers with a real Google sign-in. **CI round** (`api.yml`/`frontend.yml`: validate
on every PR + publish images to ghcr on merge to main; `dev`+`main` branches; branch
protection on `main`). Health endpoints added (`/health/live`, `/health/ready`).
**Infra round (code written, apply pending):** `infra/` Terraform (bootstrap + ACA api/web +
Log Analytics cap + budget) and `deploy` jobs (OIDC → migrate → `az containerapp update`).
Azure subscription active; `az` logged in.

**Remaining to go live (user-driven, see `infra/README.md`):** install Terraform; Neon
signup (pooled + direct strings); `terraform apply` (bootstrap then main); set GitHub
`AZURE_*` + `DATABASE_CONNECTION_STRING` secrets and `VITE_API_BASE_URL`/`AZURE_RESOURCE_GROUP`/
`ACA_API_NAME`/`ACA_WEB_NAME`/`DEPLOY_ENABLED` variables; add prod web URL to Google OAuth
origins; merge `dev`→`main` (this round's code) to trigger first deploy.

**Deferred (future):** Terraform-in-CI, Key Vault, App Insights/OTel, custom domain, MCP
server, guest/demo mode. (See `notes-app-future-enhancements` memory.)

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
