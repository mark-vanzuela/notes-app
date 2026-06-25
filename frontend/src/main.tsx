import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { Provider } from 'react-redux';
import { BrowserRouter } from 'react-router-dom';
import { GoogleOAuthProvider } from '@react-oauth/google';

import App from './App.tsx';
import { store } from './app/store.ts';
import { setUnauthorizedHandler } from './api/client.ts';
import { logout } from './features/auth/authSlice.ts';
import './index.css';

// The Google OAuth client id is baked in at build time by Vite.
const GOOGLE_CLIENT_ID = import.meta.env.VITE_GOOGLE_CLIENT_ID;

// Wire the api client's 401 handler to dispatch logout. Doing it here (rather than
// importing the store inside client.ts) avoids an import cycle.
setUnauthorizedHandler(() => store.dispatch(logout()));

// main.tsx is the entry point. We wrap <App /> in the providers it needs:
//   - <GoogleOAuthProvider>  enables the <GoogleLogin> button.
//   - <Provider store={store}>  makes Redux state available to every component.
//   - <BrowserRouter>  enables client-side routing.
createRoot(document.getElementById('root')!).render(
  <StrictMode>
    <GoogleOAuthProvider clientId={GOOGLE_CLIENT_ID}>
      <Provider store={store}>
        <BrowserRouter>
          <App />
        </BrowserRouter>
      </Provider>
    </GoogleOAuthProvider>
  </StrictMode>
);
