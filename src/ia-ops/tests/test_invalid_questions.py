import requests

WEBHOOK_URL = "https://diogocanchari.app.n8n.cloud/webhook/ai-agent-orchestrator"


def enviar_pregunta(data):
    return requests.post(
        WEBHOOK_URL,
        json=data,
        timeout=10
    )


def test_pregunta_vacia():
    response = enviar_pregunta({
        "question": ""
    })

    print("Pregunta vacía")
    print(f"HTTP: {response.status_code}")
    print(f"Respuesta: {response.text}")

    assert response.status_code == 200


def test_campo_question_inexistente():
    response = enviar_pregunta({
        "mensaje": "Hola"
    })

    print("Campo question inexistente")
    print(f"HTTP: {response.status_code}")
    print(f"Respuesta: {response.text}")

    assert response.status_code == 200


def test_json_incorrecto():
    response = requests.post(
        WEBHOOK_URL,
        data="esto no es un JSON válido",
        headers={
            "Content-Type": "application/json"
        },
        timeout=10
    )

    print("JSON incorrecto")
    print(f"HTTP: {response.status_code}")
    print(f"Respuesta: {response.text}")

    assert response.status_code == 422