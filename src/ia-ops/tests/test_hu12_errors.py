import pytest


def registrar_error(mensaje):
    """
    Simula el registro de un error producido
    durante la actualización de una fuente.
    """
    return {
        "estado": "error",
        "mensaje": mensaje
    }


def test_hu12_registro_error_actualizacion():
    """
    HU-12:
    Verifica que un error durante la actualización
    de una fuente pueda ser registrado.
    """
    resultado = registrar_error("Error al actualizar la fuente")

    assert resultado["estado"] == "error"
    assert resultado["mensaje"] != ""


def test_hu12_control_error_actualizacion():
    """
    HU-12:
    Verifica que el sistema pueda controlar el error
    sin detener la ejecución de la prueba.
    """
    try:
        resultado = registrar_error("Fuente no disponible")

        assert resultado["estado"] == "error"

    except Exception as error:
        pytest.fail(f"El error no fue controlado correctamente: {error}")