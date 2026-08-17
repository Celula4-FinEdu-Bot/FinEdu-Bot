## Descripción

<!-- Explica brevemente qué hace este PR y por qué es necesario -->

## Tipo de cambio

- [ ] Nueva funcionalidad
- [ ] Corrección de bug
- [ ] Cambio de estructura / refactor
- [ ] Cambio en flujos de n8n
- [ ] Cambio en base de datos (migración)
- [ ] Cambio en prompts / IA
- [ ] Documentación
- [ ] Otro (especificar):

## Componente(s) afectado(s)

- [ ] `src/frontend`
- [ ] `src/n8n-workflows`
- [ ] `src/database`
- [ ] `src/ia-ops`
- [ ] `infrastructure`
- [ ] `.github` (CI/CD)

## Checklist de estructura

- [ ] Mi cambio respeta la estructura de carpetas definida en el README
- [ ] No agregué archivos ni carpetas fuera de `src/`, `infrastructure/` o `.github/` en la raíz del repo
- [ ] Si agregué un flujo de n8n, está en `templates/` (no directo en `production/`)
- [ ] Si agregué una migración de base de datos, es un archivo nuevo numerado (no edité una migración existente)
- [ ] Si modifiqué un system prompt, actualicé el versionado en `src/ia-ops/prompts/`

## Checklist de seguridad (DevSecOps)

- [ ] No incluí credenciales, API keys, ni secretos reales en el código
- [ ] Si agregué una variable de entorno nueva, actualicé `.env.example`
- [ ] No subí carpetas de compilación (`bin/`, `obj/`, `node_modules/`, `dist/`)
- [ ] Si toqué algo relacionado a IA/prompts, corrí las pruebas de `src/ia-ops/tests/` (prompt injection, outputs)
- [ ] Revisé que no se expone información sensible en logs o respuestas de error

## Pruebas realizadas

<!-- ¿Cómo probaste este cambio? Build local, tests automáticos, prueba manual, etc. -->

## Checklist de CI

- [ ] `frontend-ci.yml` pasa en verde (si aplica)
- [ ] `n8n-validate-ci.yml` pasa en verde (si aplica)
- [ ] `ai-testing-ci.yml` pasa en verde (si aplica)
