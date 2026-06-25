# FLOW — how the frontend works (a learning walkthrough)

This document explains the React app from the ground up, for someone learning React +
Redux. It follows the data as it moves through the app, then covers auth, routing, and
testing. Read it top to bottom once; afterwards the code should feel familiar.

---

## 1. The big picture

```
Browser  ─►  React components  ─►  Redux store (state)  ─►  API module (fetch)  ─►  ASP.NET API  ─►  Postgres
                 ▲                        │
                 └──── re-render ◄────────┘   (components subscribe to state; state changes re-render them)
```

- **Components** render UI and dispatch actions. They never call `fetch` directly.
- **Redux store** holds all shared state (auth + notes) in one object.
- **Async thunks** do the HTTP work (via the API module) and update the store.
- **Selectors** read slices of state; components subscribe through them.

The app is split into **feature folders** (`features/auth`, `features/notes`). Each owns
its slice of state plus the components for that feature — everything one feature needs
sits together ("vertical slice").

---

## 2. Startup: `main.tsx`

`main.tsx` is the entry point. It wraps `<App/>` in three providers, outermost first:

1. **`<GoogleOAuthProvider>`** — makes the Google Sign-In button work (needs the client id).
2. **`<Provider store={store}>`** — makes the Redux store available everywhere.
3. **`<BrowserRouter>`** — enables URL-based routing.

It also wires one thing: `setUnauthorizedHandler(() => store.dispatch(logout()))`. This
tells the API client "if a request comes back 401, log the user out." (Done here, not
inside the client, to avoid an import cycle.)

---

## 3. State: Redux Toolkit slices

A **slice** = a piece of state + the reducers that change it. We have two:
`features/auth/authSlice.ts` and `features/notes/notesSlice.ts`. The store
(`app/store.ts`) combines them, so state looks like:

```ts
{ auth: { token, user, status, error },
  notes: { items, status, error, selected, selectedStatus } }
```

### Reading state — selectors + typed hooks

Components read state with `useAppSelector` (a typed `useSelector` from `app/hooks.ts`)
and a **selector** function:

```ts
const notes = useAppSelector(selectNotes); // re-renders when notes change
```

Selectors (e.g. `selectNotes`) live in the slice, so components don't reach into
`state.notes.items` directly — the state shape stays an implementation detail.

### Changing state — actions & thunks

You never mutate state directly. You **dispatch** an action; a **reducer** computes the
next state. For async work (HTTP) we use `createAsyncThunk`, which auto-dispatches three
actions — `pending`, `fulfilled`, `rejected` — that we handle in `extraReducers` to drive
loading / success / error UI:

```ts
export const fetchNotes = createAsyncThunk('notes/fetchAll', () => notesApi.getAll());
// reducer: pending -> status='loading', fulfilled -> items=payload, rejected -> error
```

A component kicks this off:

```ts
const dispatch = useAppDispatch();
useEffect(() => { dispatch(fetchNotes()); }, [dispatch]); // load once on mount
```

---

## 4. Talking to the API: `api/`

- **`client.ts`** — a small wrapper around `fetch` that every call goes through. It:
  prepends `VITE_API_BASE_URL`, attaches `Authorization: Bearer <token>` (read from
  `localStorage`), throws on non-2xx, and on **401** triggers logout.
- **`authApi.ts` / `notesApi.ts`** — thin objects mapping each operation to a route
  (`notesApi.getAll()` → `GET /notes`). Thunks call these; components never see `fetch`.

---

## 5. The auth flow (the new part vs. a plain CRUD app)

```
[Sign in with Google]  ─►  Google returns an ID token
        │
        ▼
dispatch(loginWithGoogle(idToken))  ─►  POST /api/auth/google  ─►  { token, user }
        │
        ▼
authSlice stores token + user  ──►  in Redux (for UI)  AND  localStorage (survive refresh)
        │
        ▼
every later request: client.ts attaches  Authorization: Bearer <token>
```

- **First sign-in = sign-up**: the API creates the user the first time it sees a Google
  account. The frontend doesn't care — it just gets a token back.
- **Staying signed in**: `authSlice`'s initial state reads the saved token+user from
  `localStorage`, so a page refresh keeps you logged in.
- **Logging out / expiry**: `logout` clears Redux + localStorage. A 401 from the API
  (expired token) triggers the same logout automatically.

---

## 6. Routing & protection

`App.tsx` defines the routes. Protected pages are wrapped in **`<RequireAuth>`**, which
checks `selectIsAuthenticated` and redirects to `/login` if there's no token:

```
/login            -> LoginPage   (redirects to /notes if already signed in)
/notes            -> NotesList   (guarded)
/notes/new        -> NoteForm    (guarded, create mode)
/notes/:id/edit   -> NoteForm    (guarded, edit mode — :id tells it which)
/  and  *         -> redirect to /notes
```

`NoteForm` is one component for both create and edit; it detects edit mode from the
presence of the `:id` route param.

---

## 7. Styling

Global utility classes (`.btn`, `.alert`, `.muted`, form styles) and CSS variables live
in `index.css`. Component-specific styles use **CSS Modules** (`*.module.css`): you
`import styles from './X.module.css'` and use `className={styles.card}`, which gives
locally-scoped class names (no global collisions).

---

## 8. Testing (Vitest + React Testing Library)

Run: `npm test` (watch) or `npm run test:run` (once).

- **Config** lives in `vite.config.ts` (`test: { environment: 'jsdom', globals: true,
  setupFiles: 'src/test/setup.ts' }`). jsdom simulates a browser DOM; `setup.ts` adds the
  jest-dom matchers (`toBeInTheDocument()` etc.).
- **`src/test/test-utils.tsx`** exports `renderWithProviders`, which renders a component
  inside a **fresh** store + Router + Google provider — the same environment as the real
  app, isolated per test.

Two kinds of tests here:

1. **Slice tests** (`*Slice.test.ts`) — create a store, **mock the API module** with
   `vi.mock`, dispatch a thunk, then assert on the resulting state. Example: "fetchNotes
   fulfilled → status succeeded + items populated."
2. **Component test** (`NotesList.test.tsx`) — `renderWithProviders(<NotesList/>)` with
   the API mocked; assert the rendered output ("Groceries" card appears) and behaviour
   (clicking Delete calls `notesApi.remove`).

### How to add a test

1. Create `Thing.test.ts(x)` next to the file it tests.
2. `vi.mock('../../api/xApi', () => ({ xApi: { method: vi.fn() } }))` to fake HTTP.
3. Arrange (set mock return with `vi.mocked(...).mockResolvedValue(...)`), act
   (`dispatch` or `renderWithProviders` + `userEvent`), assert (`expect(...)`).

---

## 9. Where to look for what

| I want to… | Look at |
| --- | --- |
| Change how a request is sent / the base URL | `src/api/client.ts` |
| Add an API call | `src/api/notesApi.ts` or `authApi.ts` |
| Change notes state/logic | `src/features/notes/notesSlice.ts` |
| Change the notes UI | `src/features/notes/NotesList.tsx` / `NoteForm.tsx` |
| Change sign-in behaviour | `src/features/auth/authSlice.ts` / `LoginPage.tsx` |
| Add/adjust a route | `src/App.tsx` |
| Add global styles | `src/index.css` |
