from playwright.sync_api import sync_playwright


def test_hu11_dato_invalido():
    with sync_playwright() as p:

        browser = p.chromium.launch()
        page = browser.new_page()

        # Abrir FinEdu-Bot
        page.goto("http://localhost:5204", wait_until="networkidle")

        # Verificar que la aplicación cargó
        assert page.title() != ""

        # Verificar que FinEdu-Bot está disponible
        assert page.get_by_text(
            "Monitor de Transparencia Económica y Gasto Público"
        ).is_visible()

        # Obtener campo de consulta
        caja = page.get_by_role("textbox")
        assert caja.is_visible()

        # Enviar una consulta inválida
        caja.fill("123456789 @@@ ###")

        # Verificar que el botón está disponible
        boton = page.get_by_role("button", name="Consultar")
        assert boton.is_visible()
        assert boton.is_enabled()

        # Ejecutar consulta
        boton.click()

        # Esperar respuesta
        page.wait_for_timeout(10000)

        # Verificar que el sistema no se bloqueó
        assert page.get_by_text("ESTADO", exact=True).is_visible()

        browser.close()
        
def test_hu11_dato_incompleto():
    with sync_playwright() as p:

        browser = p.chromium.launch()
        page = browser.new_page()

        # 1. Abrir FinEdu-Bot
        page.goto("http://localhost:5204", wait_until="networkidle")

        # 2. Verificar que la aplicación cargó
        assert page.title() != ""

        # 3. Obtener campo de consulta
        caja = page.get_by_role("textbox")
        assert caja.is_visible()

        # 4. Enviar una consulta incompleta
        caja.fill("¿Cuál es el presupuesto?")

        # 5. Verificar que el botón está disponible
        boton = page.get_by_role("button", name="Consultar")
        assert boton.is_visible()
        assert boton.is_enabled()

        # 6. Ejecutar consulta
        boton.click()

        # 7. Esperar respuesta
        page.wait_for_timeout(10000)

        # 8. Verificar que el sistema respondió
        assert page.get_by_text("ESTADO", exact=True).is_visible()

        browser.close()