import { setupStore } from '../../app/store';
import { authApi } from '../../api/authApi';
import { loginWithGoogle, logout, selectAuthUser, selectIsAuthenticated } from './authSlice';
import type { AuthResult } from '../../types/auth';

// Replace the real authApi with mocks so no HTTP happens. vi.mock is hoisted to
// the top of the file by Vitest, so it applies before authSlice imports authApi.
vi.mock('../../api/authApi', () => ({
  authApi: { googleLogin: vi.fn() },
}));

const authResult: AuthResult = {
  token: 'app-jwt',
  expiresAtUtc: new Date().toISOString(),
  user: { id: 'u1', email: 'ada@example.com', name: 'Ada', pictureUrl: null },
};

beforeEach(() => {
  localStorage.clear();
  vi.clearAllMocks();
});

describe('authSlice', () => {
  it('stores the token + user (and persists them) on successful Google login', async () => {
    vi.mocked(authApi.googleLogin).mockResolvedValue(authResult);
    const store = setupStore();

    await store.dispatch(loginWithGoogle('google-id-token'));

    const state = store.getState();
    expect(selectIsAuthenticated(state)).toBe(true);
    expect(selectAuthUser(state)?.email).toBe('ada@example.com');
    // Persisted so a refresh keeps the session.
    expect(localStorage.getItem('notes.token')).toBe('app-jwt');
  });

  it('clears state and storage on logout', async () => {
    vi.mocked(authApi.googleLogin).mockResolvedValue(authResult);
    const store = setupStore();
    await store.dispatch(loginWithGoogle('google-id-token'));

    store.dispatch(logout());

    const state = store.getState();
    expect(selectIsAuthenticated(state)).toBe(false);
    expect(selectAuthUser(state)).toBeNull();
    expect(localStorage.getItem('notes.token')).toBeNull();
  });

  it('records an error message when login fails', async () => {
    vi.mocked(authApi.googleLogin).mockRejectedValue(new Error('nope'));
    const store = setupStore();

    await store.dispatch(loginWithGoogle('bad-token'));

    expect(store.getState().auth.error).toBeTruthy();
    expect(selectIsAuthenticated(store.getState())).toBe(false);
  });
});
