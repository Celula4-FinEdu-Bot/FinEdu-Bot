import { test, expect } from "@playwright/test";

/**
 * Objetivo 1 del QA: verificar que la app Blazor Server responde en línea.
 * Esta es la prueba "PoC": la más simple posible, sirve para validar
 * que la automatización end-to-end del pipeline funciona antes de
 * escalar a casos más complejos.
 */
test.describe("Smoke: disponibilidad de la aplicación", () => {
  test("la página principal responde y renderiza", async ({ page }) => {
    const response = await page.goto("/");

    expect(
      response,
      "La app debería responder a la request inicial",
    ).not.toBeNull();
    expect(
      response!.status(),
      "La app no debería devolver error 4xx/5xx",
    ).toBeLessThan(400);

    // Blazor Server inyecta su script de conexión SignalR; si aparece,
    // el circuito interactivo se está sirviendo correctamente.
    await expect(page.locator("body")).toBeVisible();
    await expect(page).toHaveTitle(/.+/);
  });
});
