#!/usr/bin/env bash
# Restore: verifica, restaura PostgreSQL y n8n, y valida.
# Uso: ./restore.sh <archivo_backup>   (.tar.gz o .tar.gz.enc)
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
[ -f "$SCRIPT_DIR/.env" ] || { echo "ERROR: falta $SCRIPT_DIR/.env (copiar de .env.example)"; exit 1; }
# shellcheck disable=SC1091
source "$SCRIPT_DIR/.env"

log() { echo "[$(date +%H:%M:%S)] $*"; }
FILE="${1:-}"
[ -f "$FILE" ] || { echo "Uso: $0 <archivo_backup>"; exit 1; }
FILE="$(cd "$(dirname "$FILE")" && pwd)/$(basename "$FILE")"

WORK="$(mktemp -d)"
trap 'rm -rf "$WORK"' EXIT

# 1. Verificar integridad
log "1/6 Verificando integridad (sha256)..."
if [ -f "$FILE.sha256" ]; then
  ( cd "$(dirname "$FILE")" && sha256sum -c "$(basename "$FILE").sha256" ) || { echo "ERROR: el archivo está corrupto o fue modificado"; exit 1; }
else
  echo "AVISO: no se encontró $(basename "$FILE").sha256, se omite la verificación"
fi

# 2. Descifrar y extraer
log "2/6 Extrayendo backup..."
if [[ "$FILE" == *.enc ]]; then
  [ -n "${BACKUP_PASSPHRASE:-}" ] || { echo "ERROR: backup cifrado, definir BACKUP_PASSPHRASE en .env"; exit 1; }
  openssl enc -d -aes-256-cbc -pbkdf2 -pass env:BACKUP_PASSPHRASE -in "$FILE" -out "$WORK/backup.tar.gz" \
    || { echo "ERROR: passphrase incorrecta"; exit 1; }
  tar -xzf "$WORK/backup.tar.gz" -C "$WORK"
else
  tar -xzf "$FILE" -C "$WORK"
fi
DIR="$(find "$WORK" -maxdepth 1 -type d -name 'backup_*' | head -n1)"
[ -f "$DIR/db.dump" ] || { echo "ERROR: db.dump no encontrado en el backup"; exit 1; }
echo "-------- MANIFEST --------"; cat "$DIR/MANIFEST.txt"; echo "--------------------------"

# 3. Confirmación
echo "ATENCIÓN: se va a SOBREESCRIBIR la base '$POSTGRES_DB' en '$POSTGRES_CONTAINER'."
read -r -p "Escriba RESTAURAR para continuar: " OK
[ "$OK" = "RESTAURAR" ] || { echo "Cancelado."; exit 1; }

# 4. Restaurar PostgreSQL
log "3/6 Deteniendo n8n..."
docker stop "$N8N_CONTAINER" >/dev/null

log "4/6 Restaurando PostgreSQL..."
docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d postgres -c \
  "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname='$POSTGRES_DB' AND pid<>pg_backend_pid();" >/dev/null
docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d postgres -c "DROP DATABASE IF EXISTS \"$POSTGRES_DB\";"
docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d postgres -c "CREATE DATABASE \"$POSTGRES_DB\";"
docker exec -i "$POSTGRES_CONTAINER" pg_restore -U "$POSTGRES_USER" -d "$POSTGRES_DB" --no-owner --exit-on-error < "$DIR/db.dump"

# 5. Levantar n8n
log "5/6 Iniciando n8n..."
docker start "$N8N_CONTAINER" >/dev/null
for i in $(seq 1 30); do
  if curl -fsS "$N8N_HEALTH_URL" >/dev/null 2>&1; then OK_N8N=1; break; fi
  sleep 2
done

# 6. Validación
log "6/6 Validando..."
TABLES=$(docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -tAc "SELECT count(*) FROM information_schema.tables WHERE table_schema='public';")
WFS=$(docker exec "$POSTGRES_CONTAINER" psql -U "$POSTGRES_USER" -d "$POSTGRES_DB" -tAc "SELECT count(*) FROM workflow_entity;" 2>/dev/null || echo "n/a")
echo "Tablas restauradas : $TABLES"
echo "Flujos n8n         : $WFS"
if [ -n "${OK_N8N:-}" ]; then echo "n8n responde       : SI"; else echo "n8n responde       : NO (revisar: docker logs $N8N_CONTAINER)"; fi
echo
echo "Clave de cifrado n8n del backup (debe coincidir con la del entorno destino):"
echo "  $(cat "$DIR/n8n_encryption_key.txt" 2>/dev/null | sed 's/./*/5g')"
log "Restauración finalizada. Seguir checklist del README (paso 6)."
