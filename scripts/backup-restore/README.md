# Backup & Restore (PostgreSQL + n8n)

Carpeta: `devsecops/scripts/backup-restore/`

| Archivo | Qué hace |
|---|---|
| `backup.sh` | Genera el backup (BD + flujos n8n + versión del código) |
| `restore.sh` | Restaura el backup y valida |
| `.env.example` | Plantilla de configuración |

## Qué contiene el backup

- `db.dump`: base de datos PostgreSQL completa (incluye flujos, credenciales y ejecuciones de n8n si n8n usa esa BD).
- `n8n/workflows/*.json` y `n8n/credentials.json`: exportación portable de n8n (credenciales cifradas).
- `n8n_encryption_key.txt`: clave para descifrar las credenciales de n8n.
- `MANIFEST.txt`: fecha, rama y commit de GitHub, checksums.

> El código y la configuración del proyecto NO van en el backup: se obtienen de GitHub (commit indicado en `MANIFEST.txt`).

---

## A) Generar el backup (DevSecOps)

1. `cd devsecops/scripts/backup-restore`
2. `cp .env.example .env` y editar los valores (contenedores, usuario y BD, passphrase).
3. `./backup.sh`
4. Entregar a QA por un canal seguro (no GitHub):
   - `backup_<proyecto>_<fecha>.tar.gz(.enc)`
   - `backup_<proyecto>_<fecha>.tar.gz(.enc).sha256`
   - La passphrase, por un canal distinto al del archivo.

---

## B) Restaurar el backup (QA) — paso a paso

**Requisitos:** Docker, `git`, `openssl`, `curl`.

**Paso 1 — Obtener el código**
```bash
git clone <URL_DEL_REPO>
cd <repo>
git checkout <git_commit del MANIFEST.txt>
```

**Paso 2 — Levantar la infraestructura vacía**
```bash
docker compose up -d postgres n8n
docker ps --format '{{.Names}}'     # anotar los nombres de contenedor
```

**Paso 3 — Configurar el script**
```bash
cd devsecops/scripts/backup-restore
cp .env.example .env
nano .env     # nombres de contenedor, POSTGRES_USER, POSTGRES_DB y BACKUP_PASSPHRASE
```

**Paso 4 — Copiar el backup junto con su `.sha256`** a una carpeta, p. ej. `./backups/`.

**Paso 5 — Ejecutar la restauración**
```bash
./restore.sh ./backups/backup_<proyecto>_<fecha>.tar.gz.enc
```
El script verifica integridad, muestra el MANIFEST y pide escribir `RESTAURAR`.

**Paso 6 — Checklist de validación**
- [ ] El script termina sin errores y muestra `n8n responde: SI`.
- [ ] "Tablas restauradas" y "Flujos n8n" son mayores que 0 y coinciden con origen.
- [ ] Abrir n8n en el navegador e ingresar con el usuario esperado.
- [ ] Los flujos aparecen en la lista.
- [ ] Abrir un flujo y probar una credencial (si falla: la `N8N_ENCRYPTION_KEY` del entorno destino debe ser igual a `n8n_encryption_key.txt`).
- [ ] Ejecutar manualmente un flujo de prueba.
- [ ] Consulta de datos en la BD:
  ```bash
  docker exec <postgres> psql -U <user> -d <db> -c "SELECT count(*) FROM workflow_entity;"
  ```

## Problemas frecuentes

| Síntoma | Causa / solución |
|---|---|
| `sha256 FAILED` | Archivo corrupto o incompleto: pedir de nuevo |
| `passphrase incorrecta` | Revisar `BACKUP_PASSPHRASE` |
| Credenciales n8n no funcionan | Falta la misma `N8N_ENCRYPTION_KEY` en el docker-compose destino |
| `contenedor no está corriendo` | Revisar nombres con `docker ps` y el `.env` |
| `pg_restore` con errores de versión | Usar la misma versión de PostgreSQL que en origen |

## Seguridad

- `.env` y los backups deben estar en `.gitignore`.
- Los backups contienen credenciales: cifrar siempre y no compartir la passphrase junto al archivo.
