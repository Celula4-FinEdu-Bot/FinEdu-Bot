import requests

URL = "https://destructo32.app.n8n.cloud/webhook/188ba180-7d29-41e4-b1b3-2a978e1926b5"

def enviar(data):
    return requests.post(URL, json=data, timeout=10)

def test_pregunta_vacia():
    r = enviar({"question": ""})
    print("Pregunta vacía")
    print("HTTP:", r.status_code)
    print("Respuesta:", r.text)
    assert r.status_code == 200

def test_question_inexistente():
    r = enviar({"mensaje": "Hola"})
    print("Question inexistente")
    print("HTTP:", r.status_code)
    print("Respuesta:", r.text)
    assert r.status_code == 200

def test_json_incorrecto():
    r = requests.post(
        URL,
        data="esto no es un JSON válido",
        headers={"Content-Type": "application/json"},
        timeout=10
    )
    print("JSON incorrecto")
    print("HTTP:", r.status_code)
    print("Respuesta:", r.text)
    assert r.status_code == 422