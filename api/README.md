# API — ASP.NET Core Web API (.NET 8)

Controller-based Web API for the Notes App, backed by Postgres via EF Core 8 + Npgsql.

## Layout (target)

```
api/
├─ NotesApp.sln
├─ Dockerfile                 # multi-stage: sdk build → aspnet runtime
└─ src/
   ├─ NotesApp.Api/           # Program.cs, controllers, DbContext
   │  └─ Migrations/          # EF Core code-first migration files
   └─ NotesApp.Domain/        # entities / DTOs (kept light for the demo)
```

> **Status:** placeholder. The solution and projects are scaffolded in the API
> planning round.

## EF Core migrations (reference)

The C# solution owns the database schema — there is no separate SQL/migrations repo.

```bash
# from api/
dotnet ef migrations add <Name> --project src/NotesApp.Api
dotnet ef database update      --project src/NotesApp.Api
```

Migrations are applied during the **API deploy** against Neon (pipeline step vs
`Database.Migrate()` on startup — decided in the CI round).
