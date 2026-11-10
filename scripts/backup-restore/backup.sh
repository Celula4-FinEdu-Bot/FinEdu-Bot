#!/usr/bin/env bash
# Backup: PostgreSQL + flujos/credenciales n8n + info de versión del repo
# Uso: ./backup.sh
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
[ -f "$SCRIPT_DIR/.env" ] || { echo "ERROR: falta $SCRIPT_DIR/.env (copiar de .env.example)"; exit 1; }
# shellcheck disable=SC1091
source "$SCRIPT_DIR/.env"

log() { echo "[$(date +%H:%M:%S)] $*"; }

TS="$(date +%Y%m%d_%H%M%S)"
NAME="backup_${PROJECT_NAME}_${TS}"
mkdir -p "$BACKUP_DIR"
BACKUP_DIR="$(cd "$BACKUP_DIR" && pwd)"
WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT
mkdir -p "$WORK/$NAME"
cd "$WORK/$NAME"

# 0. Validaciones
for c in "$POSTGRES_CONTAINER" "$N8N_CONTAINER"; do
  docker ps --format '{{.Names}}' | grep -qx "$c" || { echo "ERROR: contenedor '$c' no está corriendo"; exit 1; }
done

# 1. Base de datos
log "1/5 Respaldando PostgreSQL ($POSTGRES_DB)..."
docker exec "$POSTGRES_CONTAINER" pg_dump -U "$POSTGRES_USER" -d "$POSTGRES_DB" -Fc > db.dump
[ -s db.dump ] || { echo "ERROR: db.dump vacío"; exit 1; }

# 2. Flujos y credenciales de n8n (JSON portable, credenciales siguen cifradas)
log "2/5 Exportando flujos y credenciales de n8n..."
docker exec "$N8N_CONTAINER" sh -c '
  rm -rf /tmp/n8n_bk && mkdir -p /tmp/n8n_bk/workflows &&
  n8n export:workflow --all --separate --output=/tmp/n8n_bk/workflows/ &&
  n8n export:credentials --all --output=/tmp/n8n_bk/credentials.json'
docker cp "$N8N_CONTAINER":/tmp/n8n_bk ./n8n
docker exec "$N8N_CONTAINER" rm -rf /tmp/n8n_bk

# 3. Clave de cifrado de n8n (sin ella las credenciales no se pueden descifrar)
log "3/5 Guardando clave de cifrado de n8n..."
docker exec "$N8N_CONTAINER" sh -c 'echo "${N8N_ENCRYPTION_KEY:-}"' > n8n_encryption_key.txt
[ -s n8n_encryption_key.txt ] || log "AVISO: N8N_ENCRYPTION_KEY no está en el entorno del contenedor; revisar ~/.n8n/config"

# 4. Versión del código (el código vive en GitHub)
log "4/5 Registrando versión del repositorio..."
REPO_ROOT="$(git -C "$SCRIPT_DIR" rev-parse --show-toplevel 2>/dev/null || true)"
{
  echo "fecha=$(date -Iseconds)"
  echo "proyecto=$PROJECT_NAME"
  echo "base_datos=$POSTGRES_DB"
  if [ -n "$REPO_ROOT" ]; then
    echo "git_remote=$(git -C "$REPO_ROOT" remote get-url origin 2>/dev/null || echo n/a)"
    echo "git_branch=$(git -C "$REPO_ROOT" rev-parse --abbrev-ref HEAD)"
    echo "git_commit=$(git -C "$REPO_ROOT" rev-parse HEAD)"
  fi
} > MANIFEST.txt
sha256sum db.dump n8n/credentials.json >> MANIFEST.txt

# 5. Empaquetar (+ cifrar opcional) + checksum
log "5/5 Empaquetando..."
cd "$WORK"
tar -czf "$NAME.tar.gz" "$NAME"
OUT="$BACKUP_DIR/$NAME.tar.gz"
if [ -n "${BACKUP_PASSPHRASE:-}" ]; then
  OUT="$OUT.enc"
  openssl enc -aes-256-cbc -pbkdf2 -salt -pass env:BACKUP_PASSPHRASE -in "$NAME.tar.gz" -out "$OUT"
else
  mv "$NAME.tar.gz" "$OUT"
  log "AVISO: backup SIN cifrar (contiene credenciales). Definir BACKUP_PASSPHRASE."
fi
( cd "$BACKUP_DIR" && sha256sum "$(basename "$OUT")" > "$(basename "$OUT").sha256" )

# Retención
if [ "${RETENTION_DAYS:-0}" -gt 0 ]; then
  find "$BACKUP_DIR" -name 'backup_*' -mtime +"$RETENTION_DAYS" -delete
fi

log "OK -> $OUT"
log "Entregar a QA: $(basename "$OUT") y $(basename "$OUT").sha256"
