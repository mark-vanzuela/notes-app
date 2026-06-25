// ============================================================================
// The HTTP CLIENT — one place that knows how to talk to the API.
// ============================================================================
// Responsibilities centralised here so no component/slice touches fetch directly:
//   - prepend the API base URL,
//   - attach the JWT as `Authorization: Bearer <token>` when we have one,
//   - turn a non-2xx response into a thrown Error (fetch does NOT throw on 4xx/5xx),
//   - treat 401 specially: the token is missing/expired -> trigger logout,
//   - parse JSON (or return undefined for an empty 204 response).

// Vite injects this at build time. Fallback keeps tests/local sane if unset.
const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'http://localhost:8080/api';

// We persist the JWT in localStorage so a page refresh keeps the user signed in.
const TOKEN_KEY = 'notes.token';

export function getStoredToken(): string | null {
  return localStorage.getItem(TOKEN_KEY);
}

export function setStoredToken(token: string | null): void {
  if (token) localStorage.setItem(TOKEN_KEY, token);
  else localStorage.removeItem(TOKEN_KEY);
}

// The app registers a callback (in main.tsx) that dispatches logout. Keeping it as
// a setter avoids an import cycle between this module and the Redux auth slice.
let onUnauthorized: (() => void) | null = null;
export function setUnauthorizedHandler(handler: () => void): void {
  onUnauthorized = handler;
}

/**
 * Tiny generic wrapper around fetch. <T> lets each caller say what it expects back.
 */
export async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const token = getStoredToken();

  const response = await fetch(`${API_BASE_URL}${path}`, {
    ...options,
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
      ...options?.headers,
    },
  });

  // 401 = not authenticated / token expired. Tell the app to log out, then throw.
  if (response.status === 401) {
    onUnauthorized?.();
    throw new Error('Your session has expired. Please sign in again.');
  }

  if (!response.ok) {
    throw new Error(`Request failed: ${response.status} ${response.statusText}`);
  }

  // 204 No Content (our DELETE) has no body to parse.
  if (response.status === 204) {
    return undefined as T;
  }

  return (await response.json()) as T;
}
