# FinEdu-Bot - Backup & Restore

Scripts para respaldar/restaurar los datos de PostgreSQL y el volumen persistente de n8n.

## Alcance

- **Código y configuración versionada:** se recuperan desde GitHub con `git clone`/`git pull`.
- **PostgreSQL:** se exporta con `pg_dump` en formato custom y se restaura con `pg_restore`.
- **n8n:** se respalda el volumen Docker `n8n_data`, que contiene su estado persistente (incluida su base interna y credenciales cifradas).
- **Secretos:** el archivo `.env` no se incluye en el respaldo ni en Git. El responsable debe entregarlo por un canal seguro y conservar la clave de cifrado de n8n (`N8N_ENCRYPTION_KEY`) si fue configurada externamente.

> En el `docker-compose.yml` compartido de FinEdu-Bot aparece el servicio `n8n` con el volumen `n8n_data:/home/node/.n8n`. El PostgreSQL del proyecto se configura por conexión externa/credenciales de n8n; no se asume que esté dentro de ese compose.

## Requisitos

- Linux, WSL o Git Bash con Bash.
- Docker en ejecución y acceso al volumen de n8n.
- `postgresql-client` instalado (`pg_dump`, `pg_restore`).
- Acceso a PostgreSQL y permisos suficientes para exportar/restaurar.
- Un volumen Docker destino para n8n ya creado.

## Preparación inicial (responsable del backup)

Desde la raíz del repositorio:

```bash
cp scripts/backup-restore/.env.example .env
```

Completar `DATABASE_URL`, `N8N_VOLUME` y, si corresponde, `N8N_CONTAINER`.

Identificar el volumen real:

```bash
docker volume ls
docker inspect n8n_local
```

El nombre suele llevar el prefijo del proyecto Compose, por ejemplo `nombrecarpeta_n8n_data`; no asumir que se llama literalmente `n8n_data`.

## Paso 1 - Generar el backup (responsable DevSecOps)

```bash
chmod +x scripts/backup-restore/*.sh
./scripts/backup-restore/backup.sh
```

El script detiene brevemente el contenedor `n8n_local` si está activo, exporta PostgreSQL, comprime el volumen de n8n, genera `manifest.txt` y `SHA256SUMS`, y vuelve a iniciar n8n.

La carpeta resultante queda en `backups/FECHA_UTC/`. Entregar al QA la carpeta completa por un medio seguro. No subirla al repositorio.

## Paso 2 - Preparar restauración (QA)

1. Obtener una copia del repositorio desde GitHub.
2. Instalar Docker y `postgresql-client`.
3. Recibir la carpeta de backup completa y copiarla al equipo de restauración.
4. Crear una base PostgreSQL de **pruebas/recuperación**, no usar producción.
5. Crear un volumen Docker vacío para la prueba, por ejemplo:

```bash
docker volume create finedu_qa_n8n_data
```

6. Copiar `.env.example` a `.env` y configurar `DATABASE_URL` hacia la base de pruebas, `N8N_VOLUME=finedu_qa_n8n_data` y el nombre del contenedor n8n si existe.

## Paso 3 - Ejecutar restauración (QA)

```bash
chmod +x scripts/backup-restore/*.sh
./scripts/backup-restore/restore.sh ./backups/FECHA_UTC
```

El QA debe revisar los checksums y confirmar escribiendo `RESTAURAR`. El script reemplaza los datos del destino y el contenido del volumen indicado. No ejecutarlo contra producción.

## Paso 4 - Verificar recuperación (QA)

- Confirmar que las tablas/datos esperados existen en PostgreSQL.
- Iniciar n8n (`docker start n8n_local`, si ese contenedor está creado) o levantarlo con Compose.
- Ingresar a n8n y comprobar que aparecen los workflows de FinEdu-Bot.
- Verificar credenciales/conexiones: PostgreSQL, Groq y Gemini. Las claves externas deben estar disponibles en el entorno de restauración.
- Ejecutar una prueba controlada del webhook `ai-agent-orchestrator` y una consulta de prueba sobre presupuesto.
- Registrar fecha, responsable, backup usado, resultado y evidencias.

## Prueba rápida de restauración de PostgreSQL

El workflow `backup-restore.yml` crea una base origen y otra destino efímeras, genera un respaldo, restaura y compara un registro de muestra. También prueba el empaquetado del volumen n8n con un volumen Docker temporal. No toca datos reales ni necesita secretos del proyecto.

## Consideraciones

- La restauración de PostgreSQL requiere que la base destino exista y esté accesible.
- El contenedor n8n debe estar detenido antes de restaurar el volumen.
- El volumen restaurado debe pertenecer a una versión compatible de n8n. El compose compartido usa `docker.n8n.io/n8nio/n8n:2.37.10`.
- `pg_dump` protege la base PostgreSQL configurada en `DATABASE_URL`; no respalda automáticamente otros servidores o bases.
- Este procedimiento no respalda archivos de GitHub ni sustituye Git. Tampoco exporta secretos de GitHub Actions.
