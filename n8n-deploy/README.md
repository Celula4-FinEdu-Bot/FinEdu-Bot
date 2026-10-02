# Despliegue de n8n en Vercel

Este directorio contiene el contenedor (`Dockerfile.vercel`) que despliega **n8n** en **Vercel**.

## Variables de entorno requeridas

Configúralas **directamente en el panel de Vercel** (Project Settings → Environment Variables). **Nunca** las agregues a este repositorio.

| Variable | Descripción |
|---|---|
| `N8N_ENCRYPTION_KEY` | Clave de cifrado de credenciales de n8n |
| `DB_TYPE` | Tipo de base de datos (p. ej. `postgresdb`) |
| `DB_POSTGRESDB_HOST` | Host de PostgreSQL |
| `DB_POSTGRESDB_DATABASE` | Nombre de la base de datos |
| `DB_POSTGRESDB_USER` | Usuario de la base de datos |
| `DB_POSTGRESDB_PASSWORD` | Contraseña de la base de datos |
| `WEBHOOK_URL` | URL pública que n8n usará para los webhooks |

## Despliegue

Desde dentro de esta carpeta, ejecuta:

```bash
cd n8n-deploy
npx vercel
```
