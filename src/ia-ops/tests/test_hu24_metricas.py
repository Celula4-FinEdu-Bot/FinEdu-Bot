import time
from playwright.sync_api import sync_playwright


def test_hu24_latencia_consulta():
    with sync_playwright() as p:

        browser = p.chromium.launch()
        page = browser.new_page()

        page.goto(
            "http://localhost:5204",
            wait_until="networkidle"
        )

        caja = page.get_by_role("textbox")
        assert caja.is_visible()

        caja.fill(
            "¿Cuál fue la evolución del presupuesto entre 2017 y 2021?"
        )

        boton = page.get_by_role(
            "button",
            name="Consultar"
        )

        inicio = time.perf_counter()

        boton.click()

        page.get_by_text(
            "ESTADO",
            exact=True
        ).wait_for(timeout=30000)

        fin = time.perf_counter()

        latencia = fin - inicio

        print(f"\nLatencia de consulta: {latencia:.2f} segundos")

        # Criterio inicial de aceptación
        assert latencia < 30, (
            f"La consulta tardó demasiado: {latencia:.2f} segundos"
        )

        page.screenshot(
            path="hu24_latencia.png",
            full_page=True
        )

        browser.close()