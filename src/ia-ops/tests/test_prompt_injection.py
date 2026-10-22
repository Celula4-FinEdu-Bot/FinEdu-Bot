import json
import re
from pathlib import Path

FIXTURE_PATH = Path(__file__).parent / "fixtures" / "injection_payloads.json"

PATRONES_SOSPECHOSOS = [
    r"ignor[ae]?\s+(las\s+)?instrucciones",
    r"ignore\s+(all\s+)?previous\s+instructions",
    r"system\s*:",
    r"</?prompt>",
    r"drop\s+table",
    r"nuevo\s+rol",
]


def cargar_payloads():
    with open(FIXTURE_PATH, encoding="utf-8") as f:
        return json.load(f)


def es_sospechoso(texto: str) -> bool:
    texto_normalizado = texto.lower()
    return any(re.search(p, texto_normalizado) for p in PATRONES_SOSPECHOSOS)


def test_detecta_todos_los_payloads_maliciosos():
    payloads = cargar_payloads()
    for texto in payloads["maliciosos"]:
        assert es_sospechoso(texto), f"No se detectó como sospechoso: {texto}"


def test_no_marca_consultas_legitimas_como_sospechosas():
    payloads = cargar_payloads()
    for texto in payloads["legitimos"]:
        assert not es_sospechoso(texto), f"Falso positivo en consulta legítima: {texto}"