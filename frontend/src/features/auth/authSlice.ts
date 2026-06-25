import { createSlice, createAsyncThunk } from '@reduxjs/toolkit';
import type { PayloadAction } from '@reduxjs/toolkit';

import { authApi } from '../../api/authApi';
import { getStoredToken, setStoredToken } from '../../api/client';
import type { AuthResult, User } from '../../types/auth';
import type { RootState } from '../../app/store';

/*
  ============================================================================
  AUTH SLICE — owns "who is signed in" + the JWT.
  ============================================================================
  Sign-in flow:
    1. The <GoogleLogin> button gives us a Google ID token.
    2. loginWithGoogle() sends it to the API, which returns OUR app JWT + user.
    3. We save the token (so api/client.ts can attach it) and the user, BOTH in
       Redux (for the UI) and in localStorage (so a refresh keeps you signed in).
*/

// We persist the user alongside the token (token lives under api/client's key).
const USER_KEY = 'notes.user';

function loadStoredUser(): User | null {
  const raw = localStorage.getItem(USER_KEY);
  if (!raw) return null;
  try {
    return JSON.parse(raw) as User;
  } catch {
    return null;
  }
}

interface AuthState {
  token: string | null;
  user: User | null;
  status: 'idle' | 'loading' | 'failed';
  error: string | null;
}

// Initial state HYDRATES from localStorage so the app starts already signed in
// if we have a saved session.
const initialState: AuthState = {
  token: getStoredToken(),
  user: loadStoredUser(),
  status: 'idle',
  error: null,
};

// Async thunk: exchange the Google ID token for our app session.
export const loginWithGoogle = createAsyncThunk('auth/loginWithGoogle', (idToken: string) =>
  authApi.googleLogin(idToken)
);

const authSlice = createSlice({
  name: 'auth',
  initialState,
  reducers: {
    // Clears the session everywhere: Redux + localStorage + the api client token.
    logout(state) {
      state.token = null;
      state.user = null;
      state.status = 'idle';
      state.error = null;
      setStoredToken(null);
      localStorage.removeItem(USER_KEY);
    },
  },
  extraReducers: (builder) => {
    builder
      .addCase(loginWithGoogle.pending, (state) => {
        state.status = 'loading';
        state.error = null;
      })
      .addCase(loginWithGoogle.fulfilled, (state, action: PayloadAction<AuthResult>) => {
        state.status = 'idle';
        state.token = action.payload.token;
        state.user = action.payload.user;
        // Persist so a refresh keeps the session.
        setStoredToken(action.payload.token);
        localStorage.setItem(USER_KEY, JSON.stringify(action.payload.user));
      })
      .addCase(loginWithGoogle.rejected, (state) => {
        state.status = 'failed';
        state.error = 'Google sign-in failed. Please try again.';
      });
  },
});

export const { logout } = authSlice.actions;

// ---- Selectors ----
export const selectAuthUser = (state: RootState) => state.auth.user;
export const selectIsAuthenticated = (state: RootState) => state.auth.token !== null;
export const selectAuthStatus = (state: RootState) => state.auth.status;
export const selectAuthError = (state: RootState) => state.auth.error;

export default authSlice.reducer;
