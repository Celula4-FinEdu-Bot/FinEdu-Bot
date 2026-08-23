## n8n-workflows

Esta carpeta concentra la parte de Backend-Canchari y los flujos de n8n:

- `src/` contiene el backend Express/TypeScript.
- `production/` y `templates/` mantienen los workflows de n8n.
- Los archivos de infraestructura compartida viven en la carpeta raíz `infrastructure/`.

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
	.prettierignore
	.prettierrc.json
	eslint.config.mjs
	package-lock.json
	package.json
	tsconfig.json
```
