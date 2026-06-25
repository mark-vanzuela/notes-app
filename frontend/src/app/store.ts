import { configureStore } from '@reduxjs/toolkit';

import authReducer from '../features/auth/authSlice';
import notesReducer from '../features/notes/notesSlice';

// The reducer map lives in one place so the real app store and the test store
// (see test/test-utils.tsx) stay identical.
const rootReducer = {
  auth: authReducer,
  notes: notesReducer,
};

/**
 * The STORE is the single source of truth for app state. The keys here
 * (`auth`, `notes`) are why selectors read `state.auth.*` / `state.notes.*`.
 */
export const store = configureStore({ reducer: rootReducer });

/**
 * Builds a brand-new store. Tests call this so each test starts from a clean
 * slate (no shared state leaking between tests).
 */
export function setupStore() {
  return configureStore({ reducer: rootReducer });
}

// These types are INFERRED from the store so they stay in sync with the real
// state/dispatch. We use them to build typed hooks (see hooks.ts).
export type RootState = ReturnType<typeof store.getState>;
export type AppDispatch = typeof store.dispatch;
export type AppStore = ReturnType<typeof setupStore>;
