from unittest.mock import Mock, patch
import requests


def respuesta_mock():
    response = Mock()
    response.status_code = 200
    response.text = '{"answer": "Respuesta simulada correctamente"}'
    return response


def enviar_pregunta(question):
    return requests.post(
        "http://test.local/webhook/question",
        json={"question": question},
        timeout=10,
    )


@patch("requests.post")
def test_pregunta_presupuesto(mock_post):
    mock_post.return_value = respuesta_mock()

    question = "¿Cuánto presupuesto ejecutó la municipalidad?"

    response = enviar_pregunta(question)

    print(f"Pregunta: {question}")
    print(f"HTTP: {response.status_code}")
    print(f"Respuesta: {response.text}")

    assert response.status_code == 200


@patch("requests.post")
def test_pregunta_empresa_ganadora(mock_post):
    mock_post.return_value = respuesta_mock()

    question = "¿Qué empresa ganó la licitación?"

    response = enviar_pregunta(question)

    print(f"Pregunta: {question}")
    print(f"HTTP: {response.status_code}")
    print(f"Respuesta: {response.text}")

    assert response.status_code == 200


@patch("requests.post")
def test_pregunta_obras_viales(mock_post):
    mock_post.return_value = respuesta_mock()

    question = "¿Cuánto se gastó en obras viales?"

    response = enviar_pregunta(question)

    print(f"Pregunta: {question}")
    print(f"HTTP: {response.status_code}")
    print(f"Respuesta: {response.text}")

    assert response.status_code == 200