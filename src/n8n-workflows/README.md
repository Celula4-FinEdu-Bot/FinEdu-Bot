## n8n-workflows

Esta carpeta ahora concentra la parte de Backend-Canchari:

- `src/` contiene el backend Express/TypeScript.
- `frontend/index.html` contiene la interfaz servida por el backend.
- `infrastructure/` contiene `docker-compose.yml` y `.env.example`.
- `production/` y `templates/` mantienen los workflows de n8n.

## Comandos

```bash
npm install
npm run dev
npm run lint
npm run check
```

## Estructura

```text
n8n-workflows/
	frontend/
		index.html
	infrastructure/
		.env.example
		docker-compose.yml
	production/
	src/
		app/
		application/
		config/
		domain/
		infrastructure/
		shared/
		server.ts
	templates/
	package.json
	tsconfig.json
```
