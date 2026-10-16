import json
from pathlib import Path

FIXTURE_PATH = Path(__file__).parent / "fixtures" / "sample_nlq_responses.json"

INTENTS_VALIDOS = {
    "EvolucionPresupuesto",
    "Proyectos",
    "ClasificacionGasto",
    "Financiamiento",
    "Contrataciones",
    "NoReconocido",
    "ConsultaVacia",
}


def cargar_respuestas_ejemplo():
    with open(FIXTURE_PATH, encoding="utf-8") as f:
        return json.load(f)


def test_todas_las_respuestas_tienen_los_campos_obligatorios():
    respuestas = cargar_respuestas_ejemplo()
    for r in respuestas:
        assert "success" in r
        assert "intent" in r
        assert "message" in r


def test_success_es_siempre_booleano():
    respuestas = cargar_respuestas_ejemplo()
    for r in respuestas:
        assert isinstance(r["success"], bool)


def test_intent_pertenece_al_catalogo_conocido():
    respuestas = cargar_respuestas_ejemplo()
    for r in respuestas:
        assert r["intent"] in INTENTS_VALIDOS, f"Intent desconocido: {r['intent']}"


def test_mensaje_nunca_esta_vacio():
    respuestas = cargar_respuestas_ejemplo()
    for r in respuestas:
        assert r["message"].strip() != ""


def test_respuesta_no_reconocida_es_consistente():
    respuestas = cargar_respuestas_ejemplo()
    no_reconocidas = [r for r in respuestas if r["intent"] == "NoReconocido"]
    for r in no_reconocidas:
        assert r["success"] is False, "Una consulta NoReconocido nunca debe marcar success=true"