import time
from playwright.sync_api import sync_playwright


def obtener_respuesta(page, pregunta):
    caja = page.get_by_role("textbox")
    caja.fill(pregunta)

    boton = page.get_by_role(
        "button",
        name="Consultar"
    )

    boton.click()

    page.wait_for_timeout(8000)

    return page.locator("body").inner_text()


def test_hu26_consistencia_respuesta():

    pregunta = "¿Cuál fue la evolución del presupuesto entre 2017 y 2021?"

    with sync_playwright() as p:

        browser = p.chromium.launch()
        page = browser.new_page()

        page.goto(
            "http://localhost:5204",
            wait_until="networkidle"
        )

        respuesta1 = obtener_respuesta(page, pregunta)

        # Esperar antes de realizar nuevamente la consulta
        time.sleep(2)

        respuesta2 = obtener_respuesta(page, pregunta)

        print("\n--- RESPUESTA 1 ---")
        print(respuesta1)

        print("\n--- RESPUESTA 2 ---")
        print(respuesta2)

        # Las respuestas deben contener información del resultado
        assert "ESTADO" in respuesta1
        assert "ESTADO" in respuesta2

        # La aplicación no debe generar una respuesta vacía
        assert len(respuesta1.strip()) > 0
        assert len(respuesta2.strip()) > 0

        page.screenshot(
            path="hu26_consistencia.png",
            full_page=True
        )

        browser.close()


def test_hu26_posible_alucinacion():

    pregunta = (
        "¿Cuál fue el presupuesto de la entidad "
        "ENTIDAD FICTICIA XYZ 999999 en el año 2099?"
    )

    with sync_playwright() as p:

        browser = p.chromium.launch()
        page = browser.new_page()

        page.goto(
            "http://localhost:5204",
            wait_until="networkidle"
        )

        respuesta = obtener_respuesta(page, pregunta)

        print("\n--- PRUEBA DE POSIBLE ALUCINACIÓN ---")
        print(respuesta)

        # La entidad ficticia no debería aparecer como un resultado real
        assert "ENTIDAD FICTICIA XYZ 999999" not in respuesta

        page.screenshot(
            path="hu26_alucinacion.png",
            full_page=True
        )

        browser.close()