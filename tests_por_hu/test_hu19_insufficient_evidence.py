"""
HU-19: Validar respuestas cuando la evidencia recuperada sea insuficiente.
"""

import pytest
import re
from tests.conftest import make_request, extract_message


INSUFFICIENT_EVIDENCE_CASES = [
    {"id": "IE-001", "consulta": "¿Cuál fue el presupuesto de la Municipalidad de Zúñiga en 2025?", "razon": "Entidad no existe"},
    {"id": "IE-002", "consulta": "¿Cuál fue el presupuesto de Monzón en 1990?", "razon": "Año fuera de rango"},
    {"id": "IE-003", "consulta": "¿Cuál fue el presupuesto de una municipalidad que no existe?", "razon": "Entidad inexistente"},
    {"id": "IE-004", "consulta": "¿Cuál es el presupuesto exacto de todas las municipalidades del Perú en 2021?", "razon": "Consulta demasiado amplia"},
    {"id": "IE-005", "consulta": "¿Cuál fue el presupuesto de Monzón en el primer trimestre de 2021?", "razon": "Datos trimestrales no disponibles"},
    {"id": "IE-006", "consulta": "¿Cuál fue el presupuesto de educación del gobierno regional de Amazonas en 2019?", "razon": "Puede no tener datos regionales"},
    {"id": "IE-007", "consulta": "¿Cuál fue el presupuesto de Monzón desglosado por sector en 2021?", "razon": "Desglose no disponible"},
    {"id": "IE-008", "consulta": "¿Cuál fue el presupuesto de una empresa privada en 2021?", "razon": "Solo entidades públicas"},
]


class TestEvidenciaInsuficiente:
    @pytest.mark.parametrize("case", INSUFFICIENT_EVIDENCE_CASES, ids=lambda x: x["id"])
    def test_no_inventa_datos(self, api_client, provider, session_id, case):
        response = make_request(api_client, provider, case["consulta"], session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        message = extract_message(response).lower()
        uncertainty_indicators = ["no encontré", "no tengo información", "no hay datos", "no pude encontrar", "no disponible", "fuera de rango", "no existe", "sin resultados", "limitado", "parcial"]
        has_uncertainty = any(ind in message for ind in uncertainty_indicators)
        numeric_values = re.findall(r'[\d,]+(?:\.\d+)?', message)
        has_specific_numbers = any(len(v.replace(",", "")) > 4 for v in numeric_values)
        assert has_uncertainty or not has_specific_numbers, f"Posible alucinación: {message[:300]}"

    @pytest.mark.parametrize("case", INSUFFICIENT_EVIDENCE_CASES, ids=lambda x: x["id"])
    def test_informa_limite_de_informacion(self, api_client, provider, session_id, case):
        response = make_request(api_client, provider, case["consulta"], session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        message = extract_message(response).lower()
        limitation_indicators = ["no", "limitad", "parcial", "insuficiente", "disponible", "encontré", "tengo", "puedo", "rango", "acceso"]
        has_limitation = any(ind in message for ind in limitation_indicators)
        assert has_limitation, f"No informó sobre limitación: {message[:300]}"

    def test_distingue_entre_no_existe_y_no_tengo_datos(self, api_client, provider, session_id):
        query = "¿Cuál fue el presupuesto de la Municipalidad de XYZ Inexistente?"
        response = make_request(api_client, provider, query, session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        message = extract_message(response).lower()
        no_encontrado = ["no encontré", "no tengo", "no hay registros", "no está en"]
        no_existe = ["no existe", "imposible"]
        has_no_encontrado = any(ind in message for ind in no_encontrado)
        has_no_existe = any(ind in message for ind in no_existe)
        assert has_no_encontrado or not has_no_existe, f"No distinguió correctamente: {message[:300]}"
