
import os

from playwright.sync_api import sync_playwright


def test_hu20_interfaz():
    with sync_playwright() as p:

        # En GitHub Actions no existe interfaz gráfica.
        # Localmente se abre el navegador para poder visualizar la prueba.
        headless = os.getenv("CI") == "true"

        browser = p.chromium.launch(headless=headless)

        page = browser.new_page(
            viewport={"width": 1440, "height": 900}
        )

        # 1. Abrir FinEdu-Bot
        page.goto(
            "http://localhost:5204",
            wait_until="networkidle"
        )

        # 2. Verificar que la aplicación cargó
        assert page.title() != ""

        # 3. Verificar el título principal
        assert page.get_by_text(
            "Monitor de Transparencia Económica y Gasto Público"
        ).is_visible()

        # 4. Verificar las opciones principales del menú
        assert page.get_by_text(
            "Presupuesto",
            exact=True
        ).is_visible()

        assert page.get_by_text(
            "Proyectos",
            exact=True
        ).is_visible()

        # 5. Verificar la caja de consulta
        caja = page.get_by_role("textbox")
        assert caja.is_visible()

        # 6. Realizar una consulta de prueba
        caja.fill(
            "¿Cuál es el presupuesto de la municipalidad?"
        )

        # 7. Verificar que el botón Consultar existe
        boton = page.get_by_role(
            "button",
            name="Consultar"
        )
        assert boton.is_visible()

        # 8. Captura antes de consultar
        page.screenshot(
            path="hu20_prueba.png",
            full_page=True
        )

        # 9. Ejecutar la consulta
        boton.click()

        # 10. Esperar a que la interfaz procese la consulta
        page.wait_for_timeout(10000)

        # 11. Captura después de consultar
        page.screenshot(
            path="hu20_resultado.png",
            full_page=True
        )

        # 12. Solo mantener el navegador abierto cuando
        # ejecutamos la prueba localmente.
        if not headless:
            page.wait_for_timeout(30000)

        browser.close()

