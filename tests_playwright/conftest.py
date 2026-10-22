"""
Playwright configuration for FinEdu-Bot QA tests.
Provides base_url fixture; api_request used from pytest-playwright plugin.
"""

import pytest


def extract_message_from_response(response):
    """Extrae el mensaje de la respuesta de Playwright."""
    try:
        data = response.json()
        return data.get("message", data.get("output", str(data)))
    except Exception:
        return str(response.text)


@pytest.fixture(scope="session")
def base_url():
    """URL base de la API a testear."""
    return "http://localhost:3000"