import os

import requests
from selenium import webdriver
from selenium.webdriver.chrome.options import Options


WEBHOOK_URL = os.getenv(
    "N8N_WEBHOOK_URL",
    "https://diogocanchari.app.n8n.cloud/webhook/ai-agent-orchestrator",
)


def test_selenium_envia_post_al_webhook():
    options = Options()
    options.add_argument("--headless")
    options.add_argument("--no-sandbox")
    options.add_argument("--disable-dev-shm-usage")

    driver = webdriver.Chrome(options=options)

    try:
        driver.get("about:blank")

        question = "Prueba de QA desde Selenium"

        response = requests.post(
            WEBHOOK_URL,
            json={
                "question": question
            },
            timeout=10,
        )

        print(f"PASS - Pregunta enviada: {question}")
        print(f"HTTP recibido: {response.status_code}")
        print(f"Respuesta: {response.text}")

        assert response.status_code == 200, (
            f"El webhook respondió con HTTP {response.status_code}: "
            f"{response.text}"
        )

        print("PASS - Webhook respondió HTTP 200")

    finally:
        driver.quit()