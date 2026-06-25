/// <reference types="vitest/config" />
import { defineConfig } from 'vite';
import react from '@vitejs/plugin-react';

// Vite config + Vitest (test runner) config in one file.
// https://vite.dev/config/  |  https://vitest.dev/config/
export default defineConfig({
  plugins: [react()],
  test: {
    // Tests run in a simulated browser DOM (jsdom) so we can render components.
    environment: 'jsdom',
    // Expose describe/it/expect/vi as globals (no import needed in every file).
    globals: true,
    // Runs once before each test file — registers the jest-dom matchers.
    setupFiles: './src/test/setup.ts',
  },
});
