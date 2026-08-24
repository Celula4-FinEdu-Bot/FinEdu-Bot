import os

import requests


WEBHOOK_URL = os.getenv(
    "N8N_WEBHOOK_URL",
    "http://localhost:5678/webhook-test/6b1594d1-5b27-4347-90b5-df5e757fc649",
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