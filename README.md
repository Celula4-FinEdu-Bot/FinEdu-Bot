# FinEdu-Bot

**Monitor de Transparencia Económica y Gasto Público**

Bot conversacional que permite a ciudadanos y periodistas consultar en lenguaje natural el gasto público, las licitaciones y los contratos adjudicados por municipalidades, usando datos abiertos del Estado.

> Ejemplo de consulta: _"¿Cuánto presupuesto ha ejecutado la municipalidad de mi distrito en obras viales este mes y qué empresas ganaron las licitaciones más altas?"_

---

## Índice

- [Estructura del repositorio](#estructura-del-repositorio)
- [Requisitos previos](#requisitos-previos)
- [Cómo correr el proyecto en local](#cómo-correr-el-proyecto-en-local)
- [Flujo de datos](#flujo-de-datos)
- [Autorización por rol](#autorización-por-rol)
- [Stack tecnológico](#stack-tecnológico)
- [Flujo de trabajo y control de calidad (DevSecOps)](#flujo-de-trabajo-y-control-de-calidad-devsecops)
- [Seguridad](#seguridad)
- [Observabilidad en producción](#observabilidad-en-producción)
- [Equipo (Célula)](#equipo-célula)
- [Licencia](#licencia)

---

## Estructura del repositorio

```
.github/
├── PULL_REQUEST_TEMPLATE.md      # Formulario obligatorio para abrir PRs
└── workflows/
    ├── frontend-ci.yml           # Pipeline DevSecOps: Linter y Build de la interfaz
    ├── n8n-validate-ci.yml       # Pipeline DevSecOps: Validador de JSONs de n8n
    ├── ai-testing-ci.yml         # Pipeline IA Ops: Pruebas unitarias de prompts y LLMs
    └── codeql.yml                # Pipeline DevSecOps: Análisis estático de seguridad (SAST)
src/
├── frontend/                     # Espacio base del frontend compartido del repo
├── n8n-workflows/                # Carpeta del backend y workflows n8n de Backend-Canchari
│   ├── frontend/                 # index.html servido por Express
│   ├── src/                      # Código TypeScript del backend
│   ├── package.json              # Scripts Node del backend
│   └── tsconfig.json             # Configuración TypeScript del backend
├── database/                     # Migraciones y seeders (PostgreSQL + pgvector)
└── ia-ops/                       # Prompts, pruebas de QA/seguridad y observabilidad
.gitignore
infrastructure/                   # docker-compose, Dockerfiles y .env.example del backend
LICENSE
README.md
```

## Requisitos previos

- [Node.js](https://nodejs.org/) y un gestor de paquetes (npm o yarn).
- [Git](https://git-scm.com/) para clonar el repositorio y trabajar con ramas.
- El orquestador **n8n** en ejecución (localmente o en la instancia configurada para el equipo).

## Cómo correr el proyecto en local

```bash
git clone <URL_DEL_REPOSITORIO>
cd FinEdu-Bot/src/n8n-workflows
npm install
```

Copiar `infrastructure/.env.example` como `infrastructure/.env` (en la raíz del repo) y completar los valores. El backend lee ese archivo al arrancar desde `src/n8n-workflows`. Las variables imprescindibles para el chat son `N8N_WEBHOOK_URL`, `N8N_WEBHOOK_AUTH_HEADER` y `N8N_WEBHOOK_AUTH_SECRET`; el resto de variables y sus valores de ejemplo están documentados en el propio `.env.example`.

El archivo `.env` está ignorado por Git y nunca debe subirse al repositorio.

**Autenticación de servicio (HU-44):** el Webhook del chat en n8n (`ai-agent-orchestrator`) usa Header Auth. El backend Express envía la cabecera `N8N_WEBHOOK_AUTH_HEADER` con el valor de `N8N_WEBHOOK_AUTH_SECRET` en cada petición, y n8n la valida antes de ejecutar el flujo. El secreto se genera con `openssl rand -hex 32`, se guarda solo en el `.env` (que no se sube al repo) y debe coincidir con la credencial Header Auth configurada en n8n.

Levantar el servidor:

```bash
npm run dev
```

Salida esperada:

```
> finedu-bot-backend@0.1.0 dev
> tsx watch src/server.ts

Server running on port 3000
```

Abrir el navegador en `http://localhost:3000`. Debe mostrarse la pantalla principal **"FinEdu - Asistente Inteligente"**, con el encabezado "Consultas RAG - Licitaciones MEF".

> **Nota:** el flujo completo requiere que n8n esté corriendo y accesible en la URL configurada en `N8N_WEBHOOK_URL` (el Agente de IA, el filtro de seguridad y la conexión a la base de datos vectorial PostgreSQL/PGVector viven del lado de n8n, no del backend). Actualmente no existe un `docker-compose` único que levante backend + n8n + base de datos en un solo paso; cada componente se ejecuta por separado según lo documentado arriba.

---

## Flujo de datos

El recorrido de una consulta a través del sistema, de punta a punta, es el siguiente:

1. **Frontend (usuario):** ingresa la pregunta en `home.razor`, que se envía vía HTTP POST con `chatInput` y `sessionId`.
2. **Backend (Node.js/Express):** recibe la petición, procesa el JSON y lo reenvía de forma segura al orquestador mediante una llamada al webhook. La llamada incluye una cabecera de autenticación de servicio (por defecto `X-Api-Key`, configurable con `N8N_WEBHOOK_AUTH_HEADER`) cuyo valor se lee de la variable de entorno `N8N_WEBHOOK_AUTH_SECRET`.
3. **Orquestador (n8n):** el nodo Webhook valida esa cabecera (Header Auth) y rechaza con HTTP 403 cualquier solicitud sin una credencial válida, antes de ejecutar el agente de IA o las herramientas. Si la validación es correcta, recibe los datos y asigna la sesión en memoria (Window Buffer Memory).
4. **AI Agent + LLM:** analiza la pregunta y decide si debe usar la herramienta `knowledge_base` (búsqueda semántica).
5. **PostgreSQL (PGVector Store):** busca similitudes matemáticas (embeddings) en los documentos del MEF y devuelve el contexto al Agente.
6. **AI Agent + Security Filter:** el Agente redacta la respuesta, el filtro valida que sea `SAFE`, y n8n la devuelve al Backend.
7. **Frontend (usuario):** la respuesta se renderiza en la pantalla del navegador.

## Autorización por rol

El frontend Blazor (`src/frontend/src`) restringe las páginas según el rol del usuario (HU-40).

- **Roles:** existen dos, `ADMIN` y `USER`, tomados de la tabla `app_roles` de la base de datos. Al iniciar sesión, la función `authenticate_app_user` devuelve el código de rol del usuario.
- **Sesión:** el rol viaja en el claim `ClaimTypes.Role` de la cookie de autenticación (`FinEdu.Auth`).
- **Políticas** (definidas en `Program.cs`, con las constantes de `Security/AuthRoles.cs`):
  - `AdminOnly`: usuario autenticado con rol `ADMIN`.
  - `AuthenticatedUser`: usuario autenticado con rol `ADMIN` o `USER`.

| Ruta | ADMIN | USER |
|---|:---:|:---:|
| `/` (Inicio / NLQ) | ✅ | ✅ |
| `/presupuesto` | ✅ | ✅ |
| `/contrataciones` | ✅ | ✅ |
| `/proyectos` | ✅ | ❌ |

- **Sin sesión:** cualquier ruta protegida redirige a `/login`.
- **Sin permiso:** un usuario con sesión pero sin el rol requerido ve la página `/acceso-denegado`. Además, el menú lateral oculta el enlace "Proyectos" a quien no es `ADMIN`.

**Cómo probarlo:** con el frontend en ejecución y un usuario de cada rol en la base de datos, ejecutar:

```powershell
.\src\frontend\tests\probar-autorizacion.ps1 -BaseUrl http://localhost:5000 -AdminUser <usuario-admin> -NormalUser <usuario-user>
```

El script pide las contraseñas por consola (no se guardan en ningún archivo) y muestra `OK`/`FALLO` con el código HTTP y la cabecera `Location` de cada caso.

## Stack tecnológico

**Backend & IA**

- Orquestación e IA: n8n, OpenAI API, Google Gemini API
- Backend e integración: Node.js, TypeScript, Express.js
- Base de datos y almacenamiento vectorial: PostgreSQL, PGVector
- Infraestructura y despliegue: Render, Docker Desktop, WSL 2

**Frontend**

- Orquestación e IA: NLQ (Natural Language Query)
- Patrón de arquitectura: arquitectura por capas, Service Layer, microservicios, microfrontend
- Desarrollo e integración: .NET 9, Blazor Web App, Razor, JavaScript, HttpClient
- Fuentes de datos e integración pública: API de datos abiertos del MEF, OECE, APIs REST

---

## Flujo de trabajo y control de calidad (DevSecOps)

Este proyecto sigue reglas estrictas de control de versiones, alineadas al manifiesto técnico del curso:

- **La rama `main` está protegida.** Ningún cambio se sube directo; todo nace en una rama propia (`feat/`, `fix/`, o el prefijo de rol correspondiente).
- **Todo cambio entra por Pull Request**, y requiere aprobación del **Líder DevSecOps o el Arquitecto de software** antes de integrarse.
- **El pipeline de GitHub Actions se ejecuta en cada PR** y valida, en orden:
  1. **Lint y build** del código (frontend y flujos de n8n).
  2. **CodeQL (SAST)** — análisis estático de seguridad sobre el código fuente del repositorio.
  3. **Validación de los workflows de n8n** (sintaxis, credenciales, configuración de RAG/fragmentación para PDFs de licitaciones).
  4. **Pruebas de IA Ops** sobre prompts y salidas del modelo (cuando existen tests en `src/ia-ops/tests`).
- **El botón de Merge se bloquea automáticamente** si el pipeline falla en cualquiera de los puntos anteriores.
- **Cada commit y PR debe estar vinculado a un Issue** del tablero del proyecto (GitHub Projects).

> **Nota para colaboradores:** los workflows marcados como _required check_ en Branch Protection no filtran por `paths` en su disparador `pull_request` (sí lo hacen en `push`). Si un PR no toca la carpeta relacionada con un workflow requerido y ese workflow tuviera un filtro de `paths` en `pull_request`, GitHub nunca dispara el check y el PR queda bloqueado indefinidamente en `Expected — Waiting for status`. Si agregas un nuevo workflow requerido, respeta este mismo criterio.

## Seguridad

- **Análisis estático (SAST):** CodeQL revisa automáticamente el código en busca de vulnerabilidades conocidas en cada PR y push a `main`. Los resultados quedan visibles en la pestaña _Security → Code scanning_ del repositorio.
- **Checklist de PR:** `PULL_REQUEST_TEMPLATE.md` precarga una lista de verificación mínima de estructura y seguridad que debe completarse antes de solicitar revisión.

## Observabilidad en producción

Una vez desplegado, el bot es monitoreado en tiempo real (Langfuse / Arize Phoenix) para registrar:

- Costo de tokens por consulta
- Latencia de respuesta
- Tasa de respuestas sin fuente citada (posible alucinación)

Esto es crítico porque el bot reporta información sobre gasto público real; una respuesta sin respaldo podría generar una acusación falsa contra una empresa o municipalidad.

---

## Equipo (Célula)

| Rol                         | Responsable      | Rama de trabajo      |
| --------------------------- | ---------------- | -------------------- |
| Arquitecto de software      | Samantha Lezma   | `Arquitectura-Lezma` |
| MLOps / DevSecOps Leader    | Kevin Pasion     | `DevSecOps-Pasion`   |
| Backend & IA Engineers      | Diogo Canchari   | `Backend-Canchari`   |
| Frontend & Data Interaction | Yitzak Zamudio   | `FrontEnd-Zamudio`   |
| QA & Prompt Engineer        | Alexander Marino | `QA-Marino`          |
| Scrum Master                | Alexandro Medina | `ScrumMaster-Medina` |

## Licencia

_Pendiente de definir por la célula._
