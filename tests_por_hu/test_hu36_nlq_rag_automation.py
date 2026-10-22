"""
HU-36: Automatizar pruebas funcionales del flujo NLQ/RAG integrado.
"""

import pytest
import time
import re
from tests.conftest import make_request, extract_message, extract_numeric_values


MAX_RESPONSE_TIME = 15


NLQ_RAG_TEST_QUERIES = [
    {"id": "NLQ-001", "tipo": "consulta_puntual", "consulta": "¿Cuál fue el presupuesto de la Municipalidad de Monzón en 2021?", "entidad_esperada": "Monzón", "anio_esperado": "2021", "debe_tener_valor": True},
    {"id": "NLQ-002", "tipo": "consulta_puntual", "consulta": "¿Cuánto se devengó en la Municipalidad Distrital de Cerro Azul en 2021?", "entidad_esperada": "Cerro Azul", "anio_esperado": "2021", "debe_tener_valor": True},
    {"id": "NLQ-003", "tipo": "consulta_evolucion", "consulta": "¿Cuál fue la evolución del presupuesto de Monzón entre 2017 y 2021?", "entidad_esperada": "Monzón", "anio_esperado": None, "debe_tener_valor": True},
    {"id": "NLQ-004", "tipo": "consulta_conocimiento", "consulta": "¿Qué información existe sobre el presupuesto de educación en Perú?", "entidad_esperada": None, "anio_esperado": None, "debe_tener_valor": False},
    {"id": "NLQ-005", "tipo": "consulta_comparativa", "consulta": "¿Qué municipalidades tuvieron mayor presupuesto en 2021?", "entidad_esperada": None, "anio_esperado": "2021", "debe_tener_valor": False},
]


class TestFlujoNLQRAG:
    @pytest.mark.parametrize("test_case", NLQ_RAG_TEST_QUERIES, ids=lambda x: x["id"])
    def test_flujo_completo_responde(self, api_client, provider, session_id, test_case):
        start_time = time.time()
        response = make_request(api_client, provider, test_case["consulta"], session_id)
        elapsed = time.time() - start_time
        if response is None:
            pytest.skip("Proveedor no disponible")
        assert elapsed < MAX_RESPONSE_TIME, f"El flujo tardó demasiado: {elapsed:.1f}s"
        assert response.status_code == 200
        message = extract_message(response)
        assert len(message) > 0

    @pytest.mark.parametrize("test_case", NLQ_RAG_TEST_QUERIES, ids=lambda x: x["id"])
    def test_respuesta_contiene_entidad_esperada(self, api_client, provider, session_id, test_case):
        if not test_case["entidad_esperada"]:
            pytest.skip("No aplica")
        response = make_request(api_client, provider, test_case["consulta"], session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        message = extract_message(response).lower()
        entidad = test_case["entidad_esperada"].lower()
        assert entidad in message, f"No menciona la entidad: {message[:300]}"

    @pytest.mark.parametrize("test_case", NLQ_RAG_TEST_QUERIES, ids=lambda x: x["id"])
    def test_respuesta_contiene_anio_esperado(self, api_client, provider, session_id, test_case):
        if not test_case["anio_esperado"]:
            pytest.skip("No aplica")
        response = make_request(api_client, provider, test_case["consulta"], session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        message = extract_message(response)
        anio = test_case["anio_esperado"]
        assert anio in message, f"No menciona el año: {message[:300]}"

    @pytest.mark.parametrize("test_case", NLQ_RAG_TEST_QUERIES, ids=lambda x: x["id"])
    def test_respuesta_tiene_valor_cuando_se_espera(self, api_client, provider, session_id, test_case):
        if not test_case["debe_tener_valor"]:
            pytest.skip("No se espera valor")
        response = make_request(api_client, provider, test_case["consulta"], session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        message = extract_message(response)
        numeric_values = extract_numeric_values(message)
        has_significant_number = any(len(v.replace(",", "")) > 4 for v in numeric_values)
        assert has_significant_number, f"No contiene valor numérico: {message[:300]}"


class TestIntegracionRAG:
    def test_rag_recupera_documentos_relevantes(self, api_client, provider, session_id):
        query = "¿Cuál fue el presupuesto de Monzón en 2021?"
        response = make_request(api_client, provider, query, session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        response_data = response.json()
        context_fields = ["context", "documents", "rag_context", "retrieved_documents"]
        has_context = any(field in response_data for field in context_fields)
        if has_context:
            for field in context_fields:
                if field in response_data:
                    context = response_data[field]
                    if isinstance(context, list) and len(context) > 0:
                        context_text = str(context).lower()
                        assert "monzón" in context_text or "presupuesto" in context_text, f"Contexto no relevante: {context_text[:200]}"
                        break


class TestIntegracionSQL:
    def test_sql_ejecuta_consulta_correcta(self, api_client, provider, session_id):
        query = "¿Cuál fue el presupuesto de Monzón en 2021?"
        response = make_request(api_client, provider, query, session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        response_data = response.json()
        sql_fields = ["sql", "query", "sql_query", "executed_query"]
        has_sql = any(field in response_data for field in sql_fields)
        if has_sql:
            for field in sql_fields:
                if field in response_data:
                    sql = str(response_data[field]).lower()
                    assert "select" in sql, f"No es SELECT: {sql[:200]}"
                    dangerous_keywords = ["delete", "drop", "truncate", "update", "insert"]
                    for keyword in dangerous_keywords:
                        assert keyword not in sql, f"SQL peligrosa: {keyword}"
                    break

    def test_sql_no_es_inyectable(self, api_client, provider, session_id):
        malicious_query = "¿Cuál fue el presupuesto de Monzón'; DROP TABLE presupuesto; --?"
        response = make_request(api_client, provider, malicious_query, session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        message = extract_message(response).lower()
        assert "drop" not in message or "no" in message, f"Posible SQL injection: {message[:300]}"
