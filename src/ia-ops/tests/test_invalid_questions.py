from unittest.mock import Mock, patch
import requests


def respuesta_mock(status_code, texto):
    response = Mock()
    response.status_code = status_code
    response.text = texto
    return response


@patch("requests.post")
def test_pregunta_vacia(mock_post):
    mock_post.return_value = respuesta_mock(
        200,
        '{"message": "La pregunta no puede estar vacía"}'
    )

    r = requests.post(
        "http://test.local/webhook/question",
        json={"question": ""},
        timeout=10
    )

    print("Pregunta vacía")
    print("HTTP:", r.status_code)
    print("Respuesta:", r.text)

    assert r.status_code == 200


@patch("requests.post")
def test_question_inexistente(mock_post):
    mock_post.return_value = respuesta_mock(
        200,
        '{"message": "No se encontró información para la consulta"}'
    )

    r = requests.post(
        "http://test.local/webhook/question",
        json={"mensaje": "Hola"},
        timeout=10
    )

    print("Question inexistente")
    print("HTTP:", r.status_code)
    print("Respuesta:", r.text)

    assert r.status_code == 200


@patch("requests.post")
def test_json_incorrecto(mock_post):
    mock_post.return_value = respuesta_mock(
        422,
        '{"message": "JSON inválido"}'
    )

    r = requests.post(
        "http://test.local/webhook/question",
        data="esto no es un JSON válido",
        headers={"Content-Type": "application/json"},
        timeout=10
    )

    print("JSON incorrecto")
    print("HTTP:", r.status_code)
    print("Respuesta:", r.text)

    assert r.status_code == 422