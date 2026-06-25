/// <reference types="vite/client" />

// Strongly-types the env vars we read via import.meta.env, so typos are caught
// at compile time and editors autocomplete them.
interface ImportMetaEnv {
  readonly VITE_API_BASE_URL: string;
  readonly VITE_GOOGLE_CLIENT_ID: string;
}

interface ImportMeta {
  readonly env: ImportMetaEnv;
}
