import type { ReactNode } from 'react';
import { Navigate } from 'react-router-dom';

import { useAppSelector } from '../../app/hooks';
import { selectIsAuthenticated } from './authSlice';

/**
 * A ROUTE GUARD. Wrap any protected page in <RequireAuth>...</RequireAuth>. If the
 * user is not signed in, we redirect to /login instead of rendering the children.
 *
 * `replace` swaps the history entry (so the back button doesn't return to a page
 * the user can't actually see).
 */
export function RequireAuth({ children }: { children: ReactNode }) {
  const isAuthenticated = useAppSelector(selectIsAuthenticated);

  if (!isAuthenticated) {
    return <Navigate to="/login" replace />;
  }

  return <>{children}</>;
}
