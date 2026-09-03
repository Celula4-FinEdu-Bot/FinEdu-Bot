import json
from pathlib import Path

FIXTURE_PATH = Path(__file__).parent / "fixtures" / "sample_mef_response.json"


def cargar_registro_ejemplo():
    with open(FIXTURE_PATH, encoding="utf-8") as f:
        return json.load(f)


def calcular_porcentaje_ejecucion(devengado: float, pim: float) -> float:
    if pim <= 0:
        return 0.0
    return round((devengado / pim) * 100, 2)


def test_registro_tiene_los_campos_obligatorios():
    registro = cargar_registro_ejemplo()
    campos_obligatorios = ["actividad", "ejecutora", "pia", "pim", "devengado"]
    for campo in campos_obligatorios:
        assert campo in registro, f"Falta el campo obligatorio: {campo}"


def test_montos_nunca_son_negativos():
    registro = cargar_registro_ejemplo()
    assert registro["pia"] >= 0
    assert registro["pim"] >= 0
    assert registro["devengado"] >= 0


def test_porcentaje_ejecucion_calculado_correctamente():
    registro = cargar_registro_ejemplo()
    porcentaje = calcular_porcentaje_ejecucion(registro["devengado"], registro["pim"])
    assert porcentaje == 75.0


def test_porcentaje_ejecucion_nunca_excede_100_sin_causa_valida():
    registro = cargar_registro_ejemplo()
    porcentaje = calcular_porcentaje_ejecucion(registro["devengado"], registro["pim"])
    assert 0 <= porcentaje <= 100