import type { AuthResult } from '../types/auth';
import { request } from './client';

// Auth HTTP calls. The Redux auth slice calls these; components never fetch directly.
export const authApi = {
  // Exchange a Google ID token for our app's JWT (creates the user on first sign-in).
  googleLogin: (idToken: string) =>
    request<AuthResult>('/auth/google', {
      method: 'POST',
      body: JSON.stringify({ idToken }),
    }),
};
