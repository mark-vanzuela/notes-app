# Notes App

A simple full-stack notes application — built as a demonstration of full-stack +
DevOps skills: a React frontend, a C# ASP.NET Core Web API, and a Postgres database,
all containerized and deployed to Azure.

## Architecture

```
┌────────────┐      HTTP/JSON      ┌──────────────────┐      Npgsql       ┌────────────┐
│  React SPA │ ──────────────────► │  ASP.NET Core    │ ────────────────► │  Postgres  │
│ (Vite + TS)│                     │  Web API (.NET 8)│   EF Core 8       │  (Neon/    │
└────────────┘                     └──────────────────┘                   │   local)   │
                                                                          └────────────┘
```

This is a **monorepo**. The frontend and API are **deployed independently** via
path-filtered CI — a monorepo does not force coupled deployment.

## Repository layout

| Path                | Purpose                                                        |
| ------------------- | -------------------------------------------------------------- |
| `frontend/`         | React + Vite + TypeScript single-page app                      |
| `api/`              | ASP.NET Core (.NET 8) Web API solution + EF Core migrations    |
| `infra/`            | Azure infrastructure-as-code (deferred)                        |
| `.github/workflows/`| CI/CD pipelines (path-filtered per app)                        |
| `docker-compose.yml`| Local dev: web + api + postgres                                |

## Tooling

- **Frontend:** React + Vite + TypeScript
- **API:** .NET 8 (LTS) ASP.NET Core Web API, controller-based
- **Data:** EF Core 8 + Npgsql, code-first migrations (in `api/src/NotesApp.Api/Migrations/`)
- **Database:** Postgres 16 — local via Docker Compose, Neon free tier for prod
- **Containers:** multi-stage Dockerfiles per app; Docker Compose for local dev

## Environments & deployment

- **dev** = local Docker Compose (web + api + postgres). There is no separately hosted
  dev environment in Azure.
- **`main` = production** = the single Azure environment.

Flow:

```
feature branch ──(PR to main)──► CI validates (build + test + docker build, NO deploy)
                                        │
                                  (merge to main)
                                        ▼
                          path-filtered DEPLOY to Azure
                  (frontend changed → deploy web; api changed → deploy api)
```

Each workflow also supports `workflow_dispatch` for manual / force deploys.

## Local development

> Requires Docker Desktop.

```bash
cp .env.example .env     # adjust values as needed
docker compose up        # starts web + api + postgres
```

> NOTE: app code, Dockerfiles, and Compose service definitions are scaffolded in
> later rounds. This commit establishes the repository **structure** only.

## Status

This is the initial structure scaffold. Per-app implementation (frontend, API,
migrations, containers, CI/CD, Azure infra) is planned and built in subsequent rounds.
See `docs`/per-app READMEs as they land.
