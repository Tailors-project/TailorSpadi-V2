import { defineConfig } from '@playwright/test';

export default defineConfig({
  testDir: './tests/browser',
  workers: 1,
  timeout: 180_000,
  expect: { timeout: 30_000 },
  outputDir: 'artifacts/playwright',
  use: {
    channel: 'msedge',
    baseURL: process.env.E2E_BASE_URL ?? 'https://tailorspadi.localhost:8443',
    // Use the trusted local certificate; certificate errors must fail the test.
    ignoreHTTPSErrors: false,
    trace: 'off',
  },
});
