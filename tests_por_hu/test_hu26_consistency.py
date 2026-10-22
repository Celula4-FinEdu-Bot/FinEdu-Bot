"""
T16 - Repetir consultas seleccionadas para medir consistencia
entre ejecuciones y proveedores.
"""

import pytest
import statistics
import re
from tests.conftest import make_request, extract_message, extract_numeric_values
from tests.fixtures.ground_truth import get_consistency_queries


REPETITIONS = 5
MAX_CV = 0.10


def extract_principal_value(text):
    numbers = extract_numeric_values(text)
    if not numbers:
        return None
    float_values = []
    for num in numbers:
        try:
            clean = num.replace(",", "")
            float_values.append(float(clean))
        except ValueError:
            continue
    return max(float_values) if float_values else None


def calculate_cv(values):
    if len(values) < 2:
        return 0.0
    mean = statistics.mean(values)
    if mean == 0:
        return 0.0
    std_dev = statistics.stdev(values)
    return std_dev / mean


def normalize_text(text):
    text = text.lower()
    text = re.sub(r'\s+', ' ', text)
    text = re.sub(r'[^\w\s]', '', text)
    return text.strip()


class TestConsistenciaEjecuciones:
    @pytest.mark.parametrize("query_config", get_consistency_queries(), ids=lambda x: x["id"])
    def test_consistencia_valores_numericos(self, api_client, provider, session_id, query_config):
        query = query_config["consulta"]
        valores = []
        for i in range(REPETITIONS):
            response = make_request(api_client, provider, query, f"{session_id}-{i}")
            if response is None:
                pytest.skip("Proveedor no disponible")
            assert response.status_code == 200
            message = extract_message(response)
            valor = extract_principal_value(message)
            if valor is not None:
                valores.append(valor)
        if len(valores) < 2:
            pytest.skip("No se pudieron extraer suficientes valores")
        cv = calculate_cv(valores)
        assert cv <= MAX_CV, f"Inconsistencia: {valores} (CV={cv:.2%})"

    @pytest.mark.parametrize("query_config", get_consistency_queries(), ids=lambda x: x["id"])
    def test_consistencia_entidad_mencionada(self, api_client, provider, session_id, query_config):
        query = query_config["consulta"]
        entidades = []
        for i in range(REPETITIONS):
            response = make_request(api_client, provider, query, f"{session_id}-ent-{i}")
            if response is None:
                pytest.skip("Proveedor no disponible")
            message = extract_message(response)
            found = re.findall(r'(?:municipalidad\s+(?:distrital\s+)?(?:provincial\s+)?(?:de\s+)?[A-ZÁÉÍÓÚÑ][a-záéíóúñ]+(?:\s+[A-ZÁÉÍÓÚÑ][a-záéíóúñ]+)*|ministerio\s+de\s+[a-záéíóúñ]+)', message, re.IGNORECASE)
            if found:
                entidades.append(normalize_text(found[0]))
        if len(entidades) >= 2:
            unique_entidades = set(entidades)
            assert len(unique_entidades) == 1, f"Inconsistencia en entidad: {unique_entidades}"


class TestConsistenciaEntreProveedores:
    COMPARISON_QUERIES = [
        "¿Cuál fue el presupuesto de la Municipalidad de Monzón en 2021?",
        "¿Cuánto se devengó en Cerro Azul en 2021?",
    ]

    @pytest.mark.skipif(len(__import__('tests.conftest', fromlist=['ACTIVE_PROVIDERS']).ACTIVE_PROVIDERS) < 2, reason="Se necesitan al menos 2 proveedores")
    @pytest.mark.parametrize("query", COMPARISON_QUERIES)
    def test_consistencia_entre_proveedores(self, api_client, session_id, query):
        from tests.conftest import PROVIDERS
        resultados = {}
        for provider_key, provider_config in PROVIDERS.items():
            response = make_request(api_client, provider_config, query, f"{session_id}-{provider_key}")
            if response is None:
                continue
            message = extract_message(response)
            valor = extract_principal_value(message)
            if valor is not None:
                resultados[provider_key] = valor
        if len(resultados) < 2:
            pytest.skip("No hay suficientes proveedores")
        valores = list(resultados.values())
        cv = calculate_cv(valores)
        assert cv <= 0.20, f"Inconsistencia alta: {resultados} (CV={cv:.2%})"
