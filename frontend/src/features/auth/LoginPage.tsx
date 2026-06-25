import { GoogleLogin } from '@react-oauth/google';
import { Navigate, useNavigate } from 'react-router-dom';

import { useAppDispatch, useAppSelector } from '../../app/hooks';
import {
  loginWithGoogle,
  selectIsAuthenticated,
  selectAuthStatus,
  selectAuthError,
} from './authSlice';
import styles from './LoginPage.module.css';

/**
 * The sign-in screen. The <GoogleLogin> button (from @react-oauth/google) handles
 * the Google popup and hands us back a Google ID token in `credential`. We pass
 * that to our loginWithGoogle thunk, which trades it for our app JWT.
 */
export function LoginPage() {
  const dispatch = useAppDispatch();
  const navigate = useNavigate();

  const isAuthenticated = useAppSelector(selectIsAuthenticated);
  const status = useAppSelector(selectAuthStatus);
  const error = useAppSelector(selectAuthError);

  // Already signed in? Skip the login screen.
  if (isAuthenticated) {
    return <Navigate to="/notes" replace />;
  }

  async function handleSuccess(credential?: string) {
    if (!credential) return;

    // dispatch(thunk()) returns a promise; `.match` lets us check the outcome.
    const result = await dispatch(loginWithGoogle(credential));
    if (loginWithGoogle.fulfilled.match(result)) {
      navigate('/notes', { replace: true });
    }
  }

  return (
    <div className={styles.wrap}>
      <div className={styles.card}>
        <h1>Notes</h1>
        <p className="muted">Sign in with your Google account to access your notes.</p>

        {error && <p className="alert alert--error">{error}</p>}

        <div className={styles.button}>
          <GoogleLogin onSuccess={(cred) => handleSuccess(cred.credential)} onError={() => {}} />
        </div>

        {status === 'loading' && <p className="muted">Signing in…</p>}
      </div>
    </div>
  );
}
