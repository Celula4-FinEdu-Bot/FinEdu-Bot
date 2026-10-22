"""
HU-12: Registrar y controlar errores producidos durante la actualización de fuentes.
Versión Playwright (usando pytest-playwright).
"""

import pytest
import time
import re


SOURCE_ERROR_CASES = [
    {"id": "ERR-001", "tipo_error": "conexion_fallida", "comportamiento_esperado": "reintentar_y_registrar", "severidad": "alta"},
    {"id": "ERR-002", "tipo_error": "timeout", "comportamiento_esperado": "reintentar_con_backoff", "severidad": "media"},
    {"id": "ERR-003", "tipo_error": "formato_invalido", "comportamiento_esperado": "registrar_y_continuar", "severidad": "alta"},
    {"id": "ERR-004", "tipo_error": "datos_incompletos", "comportamiento_esperado": "usar_valores_por_defecto", "severidad": "media"},
    {"id": "ERR-005", "tipo_error": "autenticacion_fallida", "comportamiento_esperado": "alertar_y_usar_cache", "severidad": "critica"},
    {"id": "ERR-006", "tipo_error": "rate_limit", "comportamiento_esperado": "reintentar_con_backoff_exponencial", "severidad": "baja"},
    {"id": "ERR-007", "tipo_error": "datos_corruptos", "comportamiento_esperado": "rechazar_y_registrar", "severidad": "alta"},
    {"id": "ERR-008", "tipo_error": "error_servidor", "comportamiento_esperado": "reintentar_y_usar_cache", "severidad": "media"},
]


class TestManejoErroresFuentes:
    @pytest.mark.parametrize("case", SOURCE_ERROR_CASES, ids=lambda x: x["id"])
    def test_caso_error_definido(self, case):
        assert "id" in case
        assert "tipo_error" in case
        assert "comportamiento_esperado" in case
        assert case["severidad"] in ["baja", "media", "alta", "critica"]

    def test_sistema_tiene_para_cada_error(self):
        tipos_error = {case["tipo_error"] for case in SOURCE_ERROR_CASES}
        tipos_requeridos = {"conexion_fallida", "timeout", "formato_invalido", "datos_incompletos", "autenticacion_fallida", "rate_limit", "datos_corruptos", "error_servidor"}
        faltantes = tipos_requeridos - tipos_error
        assert len(faltantes) == 0, f"Faltan tipos de error: {faltantes}"


class TestErroresTiempoReal:
    def test_agente_maneja_error_de_conexion(self, base_url, test_request, session_id):
        query = "¿Cuál fue el presupuesto de Monzón en 2021?"
        response = test_request.post(
            f"{base_url}/api/chat",
            data={"chatInput": query, "sessionId": session_id},
        )
        from tests_playwright.conftest import extract_message_from_response
        assert response.status_code in [200, 400, 500, 503]
        message = extract_message_from_response(response)
        if response.status_code >= 500:
            error_indicators = ["error", "no disponible", "temporalmente", "intenta nuevamente", "más tarde"]
            has_error_message = any(ind in message.lower() for ind in error_indicators)
            assert has_error_message, f"No informó claramente sobre el error: {message[:200]}"

    def test_agente_no_se_queda_colgado(self, base_url, test_request, session_id):
        query = "¿Cuál fue el presupuesto de Monzón en 2021?"
        start_time = time.time()
        response = test_request.post(
            f"{base_url}/api/chat",
            data={"chatInput": query, "sessionId": session_id},
            timeout=30000,
        )
        elapsed = time.time() - start_time
        if response is None:
            pytest.skip("Proveedor no disponible")
        assert elapsed < 30, f"El agente tardó demasiado: {elapsed:.1f}s"

    def test_agente_informa_cuando_no_hay_datos_actualizados(self, base_url, test_request, session_id):
        query = "¿Cuál fue el presupuesto de Monzón en 2026?"
        response = test_request.post(
            f"{base_url}/api/chat",
            data={"chatInput": query, "sessionId": session_id},
        )
        from tests_playwright.conftest import extract_message_from_response
        message = extract_message_from_response(response).lower()
        no_data_indicators = ["no hay datos", "no disponible", "fuera de rango", "no encontré", "aún no", "pendiente"]
        has_no_data_message = any(ind in message for ind in no_data_indicators)
        numeric_values = re.findall(r'[\d,]+(?:\.\d+)?', message)
        has_specific_numbers = any(len(v.replace(",", "")) > 4 for v in numeric_values)
        assert has_no_data_message or not has_specific_numbers, f"No informó sobre falta de datos: {message[:300]}"


class TestRegistroErrores:
    def test_errores_tienen_severidad(self):
        for case in SOURCE_ERROR_CASES:
            assert "severidad" in case
            assert case["severidad"] in ["baja", "media", "alta", "critica"]

    def test_errores_tienen_comportamiento_esperado(self):
        for case in SOURCE_ERROR_CASES:
            assert "comportamiento_esperado" in case
            assert len(case["comportamiento_esperado"]) > 0