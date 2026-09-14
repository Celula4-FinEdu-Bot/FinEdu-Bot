from playwright.sync_api import sync_playwright


def test_t36_falta_evidencia_no_inventa_datos():
    with sync_playwright() as p:
        browser = p.chromium.launch()
        page = browser.new_page()

        page.goto(
            "http://localhost:5204",
            wait_until="networkidle"
        )

        caja = page.get_by_role("textbox")
        assert caja.is_visible()

        pregunta = (
            "¿Cuánto gastó exactamente la Municipalidad Ficticia "
            "de Villa Transparencia en una obra inexistente llamada "
            "Proyecto Sol 9999 durante el año 2035?"
        )

        caja.fill(pregunta)

        boton = page.get_by_role(
            "button",
            name="Consultar"
        )

        assert boton.is_visible()
        boton.click()

        page.wait_for_timeout(10000)

        contenido = page.locator("body").inner_text()

        print("\n--- T36: FALTA DE EVIDENCIA ---")
        print(contenido)

        # Debe comunicar que no encontró evidencia/datos
        assert (
            "No se encontraron datos" in contenido
            or "no devolvió registros" in contenido
            or "No se encontró" in contenido
            or "no se encontraron" in contenido.lower()
        )

        # No debe inventar el monto solicitado
        assert "S/ 500,000" not in contenido
        assert "S/ 850,000" not in contenido

        # La respuesta debe contener información
        assert len(contenido.strip()) > 0

        page.screenshot(
            path="t36_falta_evidencia.png",
            full_page=True
        )

        browser.close()