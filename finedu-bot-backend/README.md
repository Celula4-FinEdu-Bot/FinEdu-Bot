# FinEdu Bot Backend

Base de backend para FinEdu-Bot con arquitectura por capas y preparada para integrarse con PostgreSQL, n8n y conectores externos.

## Estructura

```text
src/
npm run lint
npm run format:check
  app/                    composicion de dependencias y rutas
  config/                 variables de entorno y configuracion global
  domain/                 entidades y contratos del negocio
  application/            casos de uso y DTOs
  infrastructure/         adaptadores externos: http, db, integraciones
  shared/                 errores y tipos comunes
tests/                    pruebas del servicio
```

## Flujo recomendado

1. `domain` define reglas y contratos sin depender de frameworks.
2. `application` orquesta casos de uso.
3. `infrastructure` implementa acceso HTTP, BD y n8n.
4. `app` conecta todo y expone la API.

## Comandos

```bash
npm install
npm run dev
npm run build
```

## Siguientes módulos sugeridos

- `budget` para consulta de ejecucion presupuestal
- `procurement` para licitaciones y adjudicaciones
- `chat` para integracion NLQ con n8n y modelos IA
- `sources` para trazabilidad de fuentes y citas
