import { defineConfig, devices } from "@playwright/test";

/**
 * Configuración de Playwright para las pruebas E2E/CT del equipo de QA.
 * No modifica ni depende del código de src/frontend ni de src/n8n-workflows:
 * solo consume la app ya publicada por HTTP, como lo haría un usuario real.
 */
export default defineConfig({
  testDir: "./tests",
  fullyParallel: true,
  forbidOnly: !!process.env.CI,
  retries: process.env.CI ? 2 : 0,
  workers: process.env.CI ? 2 : undefined,
  reporter: [
    ["html", { open: "never", outputFolder: "playwright-report" }],
    ["list"],
  ],
  use: {
    baseURL: process.env.PLAYWRIGHT_BASE_URL || "http://localhost:5204",
    trace: "on-first-retry",
    screenshot: "only-on-failure",
    video: "retain-on-failure",
  },
  projects: [{ name: "chromium", use: { ...devices["Desktop Chrome"] } }],
});
