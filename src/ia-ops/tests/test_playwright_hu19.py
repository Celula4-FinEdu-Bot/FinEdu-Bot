from playwright.sync_api import sync_playwright


def test_hu19_evidencia_insuficiente():
    with sync_playwright() as p:

        browser = p.chromium.launch()
        page = browser.new_page()

        # Abrir FinEdu-Bot
        page.goto("http://localhost:5204", wait_until="networkidle")

        # Verificar que la aplicación cargó
        assert page.title() != ""

        # Obtener campo de consulta
        caja = page.get_by_role("textbox")
        assert caja.is_visible()

        # Consulta sobre una entidad inexistente
        consulta = "¿Cuál fue el PIA de la entidad XYZ ENTIDAD QUE NO EXISTE 999 en 2021?"
        caja.fill(consulta)

        # Ejecutar consulta
        boton = page.get_by_role("button", name="Consultar")
        assert boton.is_visible()
        assert boton.is_enabled()
        boton.click()

        # Esperar respuesta
        page.wait_for_timeout(10000)

        # Verificar que el sistema respondió
        assert page.get_by_text("ESTADO", exact=True).is_visible()

        # Obtener todo el texto mostrado
        contenido = page.locator("body").inner_text()

        # La entidad inexistente no debe aparecer como resultado real
        assert "XYZ ENTIDAD QUE NO EXISTE 999" not in contenido

        browser.close()