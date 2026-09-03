import { test, expect } from "@playwright/test";

/**
 * Objetivo 2 del QA: verificar que la web efectivamente consume datos
 * de la API del MEF a través de NlqService -> MefService, y no solo
 * que la página carga.
 *
 * IMPORTANTE: los selectores de abajo son un punto de partida.
 * Coordinar con el equipo de frontend para exponer atributos
 * `data-testid` estables en el input y en el contenedor de resultados,
 * en vez de depender de texto/placeholder que puede cambiar.
 * QA no debe editar directamente src/frontend: pedirlo como ticket/PR.
 */
test.describe("Integración: consulta de presupuesto vía MEF", () => {
  test("una consulta de presupuesto devuelve registros del MEF", async ({
    page,
  }) => {
    await page.goto("/");

    const input = page.getByPlaceholder(/consulta|pregunta/i);
    await input.fill("presupuesto gobierno regional");

    const boton = page.getByRole("button", {
      name: /buscar|consultar|enviar/i,
    });
    await boton.click();

    // La API del MEF puede tardar (timeout de 120s configurado en MefService),
    // por eso el timeout de la aserción es generoso.
    await expect(
      page.getByText(/registros presupuestarios/i),
      "Debería mostrarse el mensaje de éxito de NlqService con datos del MEF",
    ).toBeVisible({ timeout: 20000 });
  });

  test("una entidad inexistente no rompe la UI y muestra mensaje controlado", async ({
    page,
  }) => {
    await page.goto("/");

    const input = page.getByPlaceholder(/consulta|pregunta/i);
    await input.fill("presupuesto entidad que no existe xyz123");

    const boton = page.getByRole("button", {
      name: /buscar|consultar|enviar/i,
    });
    await boton.click();

    await expect(page.getByText(/no se encontraron registros/i)).toBeVisible({
      timeout: 20000,
    });
  });
});
