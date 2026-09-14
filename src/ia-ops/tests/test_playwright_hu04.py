from playwright.sync_api import sync_playwright


def test_hu04_entorno_productivo():
    with sync_playwright() as p:

        browser = p.chromium.launch()
        page = browser.new_page()

        # URL del entorno que se quiere validar
        page.goto("http://localhost:5204", wait_until="networkidle")

        # 1. Verificar que la aplicación cargó
        assert page.title() != ""

        # 2. Verificar que FinEdu-Bot está disponible
        assert page.get_by_text(
            "Monitor de Transparencia Económica y Gasto Público"
        ).is_visible()

        # 3. Verificar que existe el campo de consulta
        caja = page.get_by_role("textbox")
        assert caja.is_visible()

        # 4. Realizar una consulta
        caja.fill("¿Cuál es el presupuesto de la municipalidad?")

        # 5. Verificar que el botón está disponible
        boton = page.get_by_role("button", name="Consultar")
        assert boton.is_visible()
        assert boton.is_enabled()

        # 6. Ejecutar la consulta
        boton.click()

        # 7. Esperar la respuesta del sistema
        page.wait_for_timeout(10000)

        # 8. Verificar que el sistema respondió
        assert page.get_by_text("ESTADO", exact=True).is_visible()

        # 9. Verificar que la IA procesó correctamente la consulta
        assert page.get_by_text("ÉXITO", exact=True).is_visible()

        # 10. Verificar que existe una fuente de información
        assert page.get_by_text("FUENTE", exact=True).is_visible()

        browser.close()