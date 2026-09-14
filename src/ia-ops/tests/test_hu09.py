from playwright.sync_api import sync_playwright


def test_hu09_recuperacion_aplicacion():
    with sync_playwright() as p:

        browser = p.chromium.launch()
        page = browser.new_page()

        # 1. Abrir FinEdu-Bot
        page.goto("http://localhost:5204", wait_until="networkidle")

        # 2. Verificar que la aplicación está disponible
        assert page.title() != ""

        # 3. Verificar que FinEdu-Bot cargó correctamente
        assert page.get_by_text(
            "Monitor de Transparencia Económica y Gasto Público"
        ).is_visible()

        # 4. Verificar que existe el campo de consulta
        caja = page.get_by_role("textbox")
        assert caja.is_visible()

        # 5. Realizar una consulta
        caja.fill("¿Cuál es el presupuesto de la municipalidad?")

        # 6. Verificar que el botón está disponible
        boton = page.get_by_role("button", name="Consultar")
        assert boton.is_visible()
        assert boton.is_enabled()

        # 7. Ejecutar la consulta
        boton.click()

        # 8. Esperar la respuesta
        page.wait_for_timeout(10000)

        # 9. Verificar que el sistema respondió
        assert page.get_by_text("ESTADO", exact=True).is_visible()

        # 10. Verificar que la aplicación continúa operativa
        assert page.get_by_text("ÉXITO", exact=True).is_visible()

        browser.close()