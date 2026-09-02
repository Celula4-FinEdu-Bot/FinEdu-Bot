
import os

from playwright.sync_api import sync_playwright


def test_hu20_interfaz():
    with sync_playwright() as p:

        # En CI se ejecuta sin interfaz gráfica.
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

        # 4. Verificar la caja de consulta
        caja = page.get_by_role("textbox")
        assert caja.is_visible()

        # 5. Realizar consulta de datos presupuestarios
        caja.fill("Evolución 2017-2021")

        # 6. Verificar botón Consultar
        boton = page.get_by_role(
            "button",
            name="Consultar"
        )
        assert boton.is_visible()

        # 7. Ejecutar consulta
        boton.click()

        # 8. Esperar hasta que aparezca el resultado
        page.get_by_text(
            "ÉXITO",
            exact=True
        ).wait_for(
            state="visible",
            timeout=30000
        )

        # 9. Verificar que la consulta fue exitosa
        assert page.get_by_text(
            "ÉXITO",
            exact=True
        ).is_visible()

        # 10. Verificar que la fuente de datos es el MEF
        assert page.get_by_text(
            "MEF",
            exact=True
        ).first.is_visible()


        # 11. Verificar que existen resultados
        assert page.get_by_text(
            "Evolución presupuestaria 2017 - 2021"
        ).is_visible()

        assert page.get_by_text(
            "registro(s)"
        ).is_visible()

        # 12. Verificar que existe información dentro de los resultados
        assert page.get_by_text(
            "SERVICIO NAC. FORESTAL Y DE FAUNA SILVESTRE-SERFOR - SEDE CENTRAL"
        ).first.is_visible()

        # 13. Captura de evidencia de HU-20
        page.screenshot(
            path="hu20_resultado.png",
            full_page=True
        )

        # Mantener navegador abierto solamente en ejecución local
        if not headless:
            page.wait_for_timeout(30000)

        browser.close()

