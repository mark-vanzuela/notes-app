import { Routes, Route, Navigate, Link } from 'react-router-dom';

import { useAppDispatch, useAppSelector } from './app/hooks';
import { logout, selectAuthUser, selectIsAuthenticated } from './features/auth/authSlice';
import { LoginPage } from './features/auth/LoginPage';
import { RequireAuth } from './features/auth/RequireAuth';
import { NotesList } from './features/notes/NotesList';
import { NoteForm } from './features/notes/NoteForm';
import styles from './App.module.css';

/**
 * The app shell: a header shown on every page, plus the <Routes> table that swaps
 * in the component matching the current URL.
 *
 * Note routes are wrapped in <RequireAuth> so unauthenticated visitors are bounced
 * to /login. Route order matters: "/notes/new" is listed before "/notes/:id/edit"
 * so "new" is not captured as an :id.
 */
function App() {
  const dispatch = useAppDispatch();
  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const user = useAppSelector(selectAuthUser);

  return (
    <>
      <header className={styles.header}>
        <Link className={styles.brand} to="/notes">
          Notes
        </Link>

        {isAuthenticated && user && (
          <div className={styles.user}>
            {user.pictureUrl && (
              <img className={styles.avatar} src={user.pictureUrl} alt="" referrerPolicy="no-referrer" />
            )}
            <span className={styles.name}>{user.name}</span>
            <button className="btn btn--ghost" onClick={() => dispatch(logout())}>
              Sign out
            </button>
          </div>
        )}
      </header>

      <main className={styles.main}>
        <Routes>
          <Route path="/login" element={<LoginPage />} />

          <Route
            path="/notes"
            element={
              <RequireAuth>
                <NotesList />
              </RequireAuth>
            }
          />
          <Route
            path="/notes/new"
            element={
              <RequireAuth>
                <NoteForm />
              </RequireAuth>
            }
          />
          <Route
            path="/notes/:id/edit"
            element={
              <RequireAuth>
                <NoteForm />
              </RequireAuth>
            }
          />

          <Route path="/" element={<Navigate to="/notes" replace />} />
          {/* Catch-all: any unknown URL redirects back to the notes list. */}
          <Route path="*" element={<Navigate to="/notes" replace />} />
        </Routes>
      </main>
    </>
  );
}

export default App;
