"""
T15 - Comparar cada respuesta con el contexto recuperado por RAG
para identificar afirmaciones no sustentadas (alucinaciones).
"""

import pytest
import re
from tests.conftest import make_request, extract_message, extract_numeric_values


RAG_ENDPOINT = "http://localhost:5678/webhook/rag-search"
USE_AGENT_CONTEXT = True


def extract_context_from_response(response_data):
    context_fields = ["context", "documents", "rag_context", "retrieved_documents", "sources"]
    for field in context_fields:
        if field in response_data:
            context = response_data[field]
            if isinstance(context, list):
                return [str(doc) for doc in context]
            elif isinstance(context, str):
                return [context]
    return [str(response_data)]


def normalize_number(num_str):
    return num_str.replace(",", "").replace(".", "").strip()


def is_number_in_context(number, context_docs):
    normalized_number = normalize_number(number)
    for doc in context_docs:
        if number in doc:
            return True
        normalized_doc = normalize_number(doc)
        if normalized_number in normalized_doc:
            return True
    return False


def extract_claims(text):
    claims = []
    patterns = [
        r'(?:fue|es|ascendió a|alcanzó|registró|se devengó)\s+(?:de\s+)?([\d,]+(?:\.\d+)?)',
        r'presupuesto\s+(?:de|fue|es)\s+(?:S/?\.?\s*)?([\d,]+(?:\.\d+)?)',
    ]
    for pattern in patterns:
        matches = re.findall(pattern, text, re.IGNORECASE)
        claims.extend(matches)
    return claims


class TestRAGGrounding:
    TEST_QUERIES = [
        "¿Cuál fue el presupuesto de la Municipalidad de Monzón en 2021?",
        "¿Cuánto se devengó en Cerro Azul en 2021?",
        "¿Cuál es la evolución del presupuesto de educación entre 2017 y 2021?",
    ]

    @pytest.mark.parametrize("query", TEST_QUERIES)
    def test_respuesta_sustentada_en_contexto(self, api_client, provider, session_id, query):
        response = make_request(api_client, provider, query, session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        assert response.status_code == 200
        response_data = response.json()
        message = extract_message(response)
        if USE_AGENT_CONTEXT:
            context_docs = extract_context_from_response(response_data)
        else:
            rag_response = api_client.post(RAG_ENDPOINT, json={"query": query}, timeout=10)
            context_docs = extract_context_from_response(rag_response.json())
        assert len(context_docs) > 0, "No se recuperó contexto RAG"
        numeric_values = extract_numeric_values(message)
        unsustained_claims = []
        for value in numeric_values:
            if len(value.replace(",", "")) <= 4:
                continue
            if not is_number_in_context(value, context_docs):
                unsustained_claims.append(value)
        if unsustained_claims:
            pytest.fail(f"Afirmaciones no sustentadas: {unsustained_claims}. Respuesta: {message[:500]}")

    def test_no_alucinaciones_en_consulta_desconocida(self, api_client, provider, session_id):
        query = "¿Cuál fue el presupuesto de la Municipalidad de Zúñiga en 2025?"
        response = make_request(api_client, provider, query, session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        message = extract_message(response).lower()
        uncertainty_indicators = ["no encontré", "no tengo información", "no hay datos", "no pude encontrar", "no disponible"]
        has_uncertainty = any(indicator in message for indicator in uncertainty_indicators)
        numeric_values = extract_numeric_values(message)
        has_specific_numbers = any(len(v.replace(",", "")) > 4 for v in numeric_values)
        assert has_uncertainty or not has_specific_numbers, f"Posible alucinación: {message[:300]}"
