import type { ReactElement, ReactNode } from 'react';
import { render } from '@testing-library/react';
import { Provider } from 'react-redux';
import { MemoryRouter } from 'react-router-dom';
import { GoogleOAuthProvider } from '@react-oauth/google';

import { setupStore } from '../app/store';
import type { AppStore } from '../app/store';

/**
 * A custom render that wraps the component under test in the same providers the
 * real app uses (Redux store + Router + Google provider). Each call gets a FRESH
 * store so tests don't leak state into each other.
 *
 * Returns the store too, so a test can assert on resulting state or pre-dispatch
 * actions. `route` lets a test start on a specific URL.
 *
 * Pattern from the Redux Toolkit "Writing Tests" docs.
 */
export function renderWithProviders(
  ui: ReactElement,
  { store = setupStore(), route = '/' }: { store?: AppStore; route?: string } = {}
) {
  function Wrapper({ children }: { children: ReactNode }) {
    return (
      <GoogleOAuthProvider clientId="test-client-id">
        <Provider store={store}>
          <MemoryRouter initialEntries={[route]}>{children}</MemoryRouter>
        </Provider>
      </GoogleOAuthProvider>
    );
  }

  return { store, ...render(ui, { wrapper: Wrapper }) };
}
