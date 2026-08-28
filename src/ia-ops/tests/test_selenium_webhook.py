
import os

import requests
from selenium import webdriver
from selenium.webdriver.chrome.options import Options


WEBHOOK_URL = os.getenv("N8N_WEBHOOK_URL")


def test_selenium_hello_world():
    options = Options()
    options.add_argument("--headless")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")

    driver = webdriver.Chrome(options=options)

    try:
        # Si no existe una URL de n8n configurada,
        # se verifica solamente que Selenium pueda iniciar Chrome.
        if not WEBHOOK_URL:
            assert driver is not None
            return

        response = requests.post(
            WEBHOOK_URL,
            json={"message": "Hello World"},
            timeout=10,
        )

        assert response.status_code == 200, (
            f"Error HTTP {response.status_code}: {response.text}"
        )

    finally:
        driver.quit()

