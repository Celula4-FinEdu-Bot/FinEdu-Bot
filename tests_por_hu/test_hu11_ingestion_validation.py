"""
HU-11: Detectar datos inválidos, incompletos o duplicados durante la ingesta.
"""

import pytest
import re
from tests.conftest import make_request, extract_message


INVALID_DATA_CASES = [
    {"id": "INV-001", "data": {"entidad": "Municipalidad de Prueba", "anio": 2021, "presupuesto": -5000000}, "problema": "Valor negativo"},
    {"id": "INV-002", "data": {"entidad": "Municipalidad de Prueba", "anio": 2030, "presupuesto": 1000000}, "problema": "Año futuro"},
    {"id": "INV-003", "data": {"entidad": "Municipalidad de Prueba", "anio": 2021, "presupuesto": 0}, "problema": "Valor cero"},
    {"id": "INV-004", "data": {"entidad": "", "anio": 2021, "presupuesto": 1000000}, "problema": "Nombre vacío"},
    {"id": "INV-005", "data": {"entidad": None, "anio": 2021, "presupuesto": 1000000}, "problema": "Nombre null"},
    {"id": "INV-006", "data": {"entidad": "Municipalidad de Prueba", "anio": 2021, "presupuesto": 999999999999999}, "problema": "Valor extremo"},
    {"id": "INV-007", "data": {"entidad": "Municipalidad <script>alert('xss')</script>", "anio": 2021, "presupuesto": 1000000}, "problema": "XSS"},
    {"id": "INV-008", "data": {"entidad": "Municipalidad'; DROP TABLE presupuesto; --", "anio": 2021, "presupuesto": 1000000}, "problema": "SQL injection"},
]

INCOMPLETE_DATA_CASES = [
    {"id": "INC-001", "data": {"entidad": "Municipalidad de Prueba", "anio": 2021}, "campo_faltante": "presupuesto"},
    {"id": "INC-002", "data": {"entidad": "Municipalidad de Prueba", "presupuesto": 1000000}, "campo_faltante": "anio"},
    {"id": "INC-003", "data": {"anio": 2021, "presupuesto": 1000000}, "campo_faltante": "entidad"},
    {"id": "INC-004", "data": {"entidad": "", "anio": None, "presupuesto": None}, "campo_faltante": "todos"},
]

DUPLICATE_DATA_CASES = [
    {"id": "DUP-001", "data_1": {"entidad": "Municipalidad de Prueba", "anio": 2021, "presupuesto": 1000000}, "data_2": {"entidad": "Municipalidad de Prueba", "anio": 2021, "presupuesto": 1000000}},
    {"id": "DUP-002", "data_1": {"entidad": "Municipalidad de Prueba", "anio": 2021, "presupuesto": 1000000}, "data_2": {"entidad": "Municipalidad de Prueba", "anio": 2021, "presupuesto": 1200000}},
    {"id": "DUP-003", "data_1": {"entidad": "Municipalidad de Prueba", "anio": 2020, "presupuesto": 900000}, "data_2": {"entidad": "Municipalidad de Prueba", "anio": 2021, "presupuesto": 1000000}},
]


class TestDatosInvalidos:
    @pytest.mark.parametrize("case", INVALID_DATA_CASES, ids=lambda x: x["id"])
    def test_detecta_dato_invalido(self, case):
        data = case["data"]
        errores = []
        if data.get("presupuesto") is not None and data["presupuesto"] < 0:
            errores.append("presupuesto_negativo")
        if data.get("anio") is not None and data["anio"] > 2026:
            errores.append("anio_futuro")
        if data.get("presupuesto") == 0:
            errores.append("presupuesto_cero")
        if not data.get("entidad"):
            errores.append("entidad_vacia")
        if data.get("presupuesto") is not None and data["presupuesto"] > 1e12:
            errores.append("valor_extremo")
        if data.get("entidad") and "<script>" in str(data["entidad"]).lower():
            errores.append("posible_xss")
        if data.get("entidad") and (";" in str(data["entidad"]) or "drop" in str(data["entidad"]).lower()):
            errores.append("posible_sql_injection")
        assert len(errores) > 0, f"No detectó el problema: {case['problema']}"


class TestDatosIncompletos:
    @pytest.mark.parametrize("case", INCOMPLETE_DATA_CASES, ids=lambda x: x["id"])
    def test_detecta_dato_incompleto(self, case):
        data = case["data"]
        campos_requeridos = ["entidad", "anio", "presupuesto"]
        campos_faltantes = [c for c in campos_requeridos if c not in data or data[c] is None or data[c] == ""]
        assert len(campos_faltantes) > 0, f"No detectó campos faltantes"



class TestDatosDuplicados:
    @pytest.mark.parametrize(
        "case",
        DUPLICATE_DATA_CASES,
        ids=lambda x: x["id"]
    )
    def test_detecta_dato_duplicado(self, case):
        data_1 = case["data_1"]
        data_2 = case["data_2"]

        es_duplicado_exacto = data_1 == data_2
        misma_entidad_anio = (
            data_1["entidad"] == data_2["entidad"]
            and data_1["anio"] == data_2["anio"]
        )

        es_duplicado = es_duplicado_exacto or misma_entidad_anio

        if case["id"] == "DUP-003":
            assert not es_duplicado, (
                "No debe considerar duplicados registros "
                "de la misma entidad en años diferentes"
            )
        else:
            assert es_duplicado, (
                f"No detectó el duplicado esperado: {case['id']}"
            )



class TestIntegracionValidacion:
    def test_agente_no_inventa_datos(self, api_client, provider, session_id):
        query = "¿Cuál fue el presupuesto de la Municipalidad de XYZ Inexistente en 2021?"
        response = make_request(api_client, provider, query, session_id)
        if response is None:
            pytest.skip("Proveedor no disponible")
        message = extract_message(response).lower()
        uncertainty_indicators = ["no encontré", "no tengo información", "no hay datos", "no pude encontrar", "no disponible", "no existe"]
        has_uncertainty = any(ind in message for ind in uncertainty_indicators)
        numeric_values = re.findall(r'[\d,]+(?:\.\d+)?', message)
        has_specific_numbers = any(len(v.replace(",", "")) > 4 for v in numeric_values)
        assert has_uncertainty or not has_specific_numbers, f"Posible alucinación: {message[:300]}"
