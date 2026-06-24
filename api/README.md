# API — ASP.NET Core Web API (.NET 8)

Controller-based Web API for the Notes App: notes CRUD + Google sign-in, backed by
Postgres via EF Core 8 + Npgsql. Clean architecture (CQRS/MediatR, FluentValidation,
ProblemDetails), mirroring the `CustomerApi` conventions.

## Layout

```
api/
├─ NotesApp.sln
├─ Dockerfile                       # multi-stage: sdk build → aspnet runtime
├─ src/
│  ├─ NotesApp.Api/                 # Program.cs, controllers, auth, middleware, settings
│  ├─ NotesApp.Application/         # CQRS features, abstractions, DTOs, validators
│  ├─ NotesApp.Domain/              # Note + User entities
│  └─ NotesApp.Infrastructure/      # EF Core DbContext, repositories, Google/JWT, Migrations/
└─ tests/
   └─ NotesApp.UnitTests/           # xUnit + FluentAssertions + Moq
```

## Run

```bash
# build + test (from api/)
dotnet build
dotnet test

# full stack (from repo root) — API + Postgres
docker compose up --build db api
# Swagger: http://localhost:8080/swagger
```

In Development the API auto-applies EF Core migrations on startup, so the database is
ready after `docker compose up`.

## Auth

`POST /api/auth/google` exchanges a Google ID token for the app's JWT; all `api/notes`
endpoints require `Authorization: Bearer <jwt>`. To exercise protected endpoints in
Swagger, mint a Google ID token (OAuth Playground), POST it to `/api/auth/google`, then
paste the returned JWT into Swagger's **Authorize** box. Config keys live in
`.env.example` / `appsettings*.json` (`Authentication:Google:ClientId`, `Jwt:*`).

## EF Core migrations

The C# solution owns the database schema — migrations live in the Infrastructure project
(the DbContext's assembly). There is no separate SQL/migrations repo.

```bash
# from api/
dotnet ef migrations add <Name> --project src/NotesApp.Infrastructure --startup-project src/NotesApp.Api
dotnet ef database update       --project src/NotesApp.Infrastructure --startup-project src/NotesApp.Api
```

In production, migrations are applied during the API deploy against Neon (pipeline step
vs `Database.Migrate()` on startup — decided in the infra/CI round).
