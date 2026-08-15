# FinEdu-Bot

**Monitor de Transparencia Económica y Gasto Público**

Bot conversacional que permite a ciudadanos y periodistas consultar en lenguaje natural el gasto público, las licitaciones y los contratos adjudicados por municipalidades, usando datos abiertos del Estado.

> Ejemplo de consulta: _"¿Cuánto presupuesto ha ejecutado la municipalidad de mi distrito en obras viales este mes y qué empresas ganaron las licitaciones más altas?"_

---

## Arquitectura Lezma
- **Frontend:** microfrontend ligero, consulta en lenguaje natural (NLQ)
- **Orquestación:** n8n self-hosted (Docker Compose) como backend/API central
- **IA:** multi-modelo con failover automático (ej. OpenAI → Anthropic)
- **Fuente de datos:** portales de Open Data / contratación pública (APIs y scrapers)
- **Base de datos:** PostgreSQL + pgvector
- **Observabilidad:** Langfuse / Arize Phoenix

_(Diagrama de arquitectura completo: ver `/docs/arquitectura.drawio`)_

## Estructura del repositorio

```
/frontend           → microfrontend (NLQ)
/n8n-workflows       → flujos exportados (JSON)
/connectors          → scrapers y conectores a portales open data
/infra               → docker-compose, configuración de despliegue
/docs                → diagramas y documentación técnica
```

## Cómo correr el proyecto en local

```bash
git clone <url-del-repo>
cd finedu-bot
docker compose up -d
```

Esto levanta n8n, la base de datos y el microfrontend en el entorno de desarrollo.

---

## Flujo de trabajo y control de calidad (DevSecOps)

Este proyecto sigue reglas estrictas de control de versiones, alineadas al manifiesto técnico del curso:

- **La rama `main` está protegida.** Ningún cambio se sube directo; todo nace en una rama propia.
- **Todo cambio entra por Pull Request**, y requiere aprobación del **Líder DevSecOps o el Arquitecto de software** antes de integrarse.
- **El pipeline de GitHub Actions se ejecuta en cada PR** y valida, en orden:
  1. Lint del código
  2. **Análisis de seguridad estático (SAST)** sobre los conectores a los portales públicos
  3. **Validación de los workflows de n8n** (sintaxis, credenciales, configuración de RAG/fragmentación para PDFs de licitaciones)
- **El botón de Merge se bloquea automáticamente** si el pipeline falla por cualquiera de los puntos anteriores.
- **Cada commit y PR debe estar vinculado a un Issue** del tablero del proyecto (GitHub Projects).

## Observabilidad en producción

Una vez desplegado, el bot es monitoreado en tiempo real (Langfuse / Arize Phoenix) para registrar:

- Costo de tokens por consulta
- Latencia de respuesta
- Tasa de respuestas sin fuente citada (posible alucinación)

Esto es crítico porque el bot reporta información sobre gasto público real; una respuesta sin respaldo podría generar una acusación falsa contra una empresa o municipalidad.

---

## Equipo (Célula)

| Rol                             | Responsabilidad  |
| ------------------------------- | ---------------- |
| Arquitecto de software          | Samantha Lezma   |
| MLOps / DevSecOps Leader        | Kevin Pasion     |
| Backend & IA Engineers          | Diogo Canchari   |
| Frontend & Data Interaction     | Yitzak Zamudio   |
| QA & Prompt Engineer            | Alexander Marino |
| Scrum Master                    | Alexandro Medina |

## Licencia

_Pendiente de definir por la célula._
