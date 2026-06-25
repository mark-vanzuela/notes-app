# Frontend — React + Vite + TypeScript

Single-page app for the Notes App: Google sign-in, then a per-user notes UI (card
grid). Talks to the ASP.NET API. State via **Redux Toolkit**; tests via **Vitest +
React Testing Library**.

New to the codebase? Read **[FLOW.md](FLOW.md)** — a guided walkthrough of how the
pieces fit together (data flow, auth, Redux, testing).

## Quickstart

```bash
cd frontend
npm install
cp .env.example .env      # then fill in the values (see below)
npm run dev               # http://localhost:5173
```

You also need the API running (from the repo root): `docker compose up db api`.

## Environment (`.env`)

Vite only exposes vars prefixed `VITE_`, and **bakes them in at build time** (they
ship in the bundle — not secret).

| Var | Meaning |
| --- | --- |
| `VITE_API_BASE_URL` | API base, e.g. `http://localhost:8080/api` |
| `VITE_GOOGLE_CLIENT_ID` | Google OAuth Web client id (`*.apps.googleusercontent.com`) |

> **Real sign-in needs a Google Client ID** set here AND on the API
> (`Authentication:Google:ClientId`) — they must match (the API checks the token's
> audience). The OAuth client's **Authorized JavaScript origin** must include
> `http://localhost:5173`. Create it in Google Cloud Console → APIs & Services →
> Credentials → OAuth client ID → *Web application*.

## Scripts

| Command | What it does |
| --- | --- |
| `npm run dev` | Vite dev server with hot reload |
| `npm run build` | Type-check (`tsc -b`) + production build to `dist/` |
| `npm run preview` | Serve the production build locally |
| `npm test` | Run Vitest in watch mode |
| `npm run test:run` | Run all tests once (CI mode) |
| `npm run lint` | Lint with oxlint |

## Containerized

A multi-stage `Dockerfile` builds the app and serves it via nginx. The repo-root
`docker-compose.yml` wires it as the `web` service:

```bash
# from repo root — whole stack (web + api + db + pgadmin)
docker compose up --build
# app at http://localhost:5173
```

## Project structure

```
src/
├─ app/         store + typed hooks
├─ api/         client (fetch wrapper) + authApi + notesApi
├─ types/       Note, User, AuthResult interfaces
├─ features/
│  ├─ auth/     authSlice, LoginPage, RequireAuth guard
│  └─ notes/    notesSlice, NotesList (cards), NoteForm
├─ test/        Vitest setup + renderWithProviders helper
├─ App.tsx      header + routes
└─ main.tsx     providers (Google + Redux + Router) + entry
```
