# FinEdu-Bot

**Monitor de Transparencia Económica y Gasto Público**

Bot conversacional que permite a ciudadanos y periodistas consultar en lenguaje natural el gasto público, las licitaciones y los contratos adjudicados por municipalidades, usando datos abiertos del Estado.

> Ejemplo de consulta: _"¿Cuánto presupuesto ha ejecutado la municipalidad de mi distrito en obras viales este mes y qué empresas ganaron las licitaciones más altas?"_

---

## Estructura del repositorio

```
.github/
├── PULL_REQUEST_TEMPLATE.md      # Formulario obligatorio para abrir PRs
└── workflows/
    ├── frontend-ci.yml           # Pipeline DevSecOps: Linter y Build de la interfaz
    ├── n8n-validate-ci.yml       # Pipeline DevSecOps: Validador de JSONs de n8n
    └── ai-testing-ci.yml         # Pipeline IA Ops: Pruebas unitarias de prompts y LLMs

src/
├── frontend/                     # Microfrontend (NLQ)
├── n8n-workflows/                # Flujos exportados (JSON) - producción y templates
├── database/                     # Migraciones y seeders (PostgreSQL + pgvector)
└── ia-ops/                       # Prompts, pruebas de QA/seguridad y observabilidad

infrastructure/
├── docker-compose.yml            # Levanta n8n, PostgreSQL y la app local
└── .env.example                  # Plantilla de variables de entorno

.gitignore
LICENSE
README.md
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

| Rol                         | Responsabilidad  |
| --------------------------- | ---------------- |
| Arquitecto de software      | Samantha Lezma   |
| MLOps / DevSecOps Leader    | Kevin Pasion     |
| Backend & IA Engineers      | Diogo Canchari   |
| Frontend & Data Interaction | Yitzak Zamudio   |
| QA & Prompt Engineer        | Alexander Marino |
| Scrum Master                | Alexandro Medina |

## Licencia

_Pendiente de definir por la célula._
