import os

import requests


WEBHOOK_URL = os.getenv(
    "WEBHOOK_URL",
    "https://diogocanchari.app.n8n.cloud/webhook/ai-agent-orchestrator"
)

def enviar_pregunta(question):
    response = requests.post(
        WEBHOOK_URL,
        json={"question": question},
        timeout=10,
    )

    return response


def test_pregunta_presupuesto():
    question = "¿Cuánto presupuesto ejecutó la municipalidad?"

    response = enviar_pregunta(question)

    print(f"Pregunta: {question}")
    print(f"HTTP: {response.status_code}")
    print(f"Respuesta: {response.text}")

    assert response.status_code == 200


def test_pregunta_empresa_ganadora():
    question = "¿Qué empresa ganó la licitación?"

    response = enviar_pregunta(question)

    print(f"Pregunta: {question}")
    print(f"HTTP: {response.status_code}")
    print(f"Respuesta: {response.text}")

    assert response.status_code == 200


def test_pregunta_obras_viales():
    question = "¿Cuánto se gastó en obras viales?"

    response = enviar_pregunta(question)

    print(f"Pregunta: {question}")
    print(f"HTTP: {response.status_code}")
    print(f"Respuesta: {response.text}")

    assert response.status_code == 200