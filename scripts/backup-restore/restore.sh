#!/usr/bin/env bash
set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"
ENV_FILE="${ENV_FILE:-$REPO_ROOT/.env}"

if [[ -f "$ENV_FILE" ]]; then
  set -a
  # shellcheck disable=SC1090
  source "$ENV_FILE"
  set +a
fi

BACKUP_DIR="${1:-}"
if [[ -z "$BACKUP_DIR" ]]; then
  echo "Uso: ./scripts/backup-restore/restore.sh <ruta-del-backup>" >&2
  exit 2
fi
BACKUP_DIR="$(cd -- "$BACKUP_DIR" && pwd)"

: "${DATABASE_URL:?Configura DATABASE_URL apuntando a la base de datos DESTINO}"
: "${N8N_VOLUME:?Configura N8N_VOLUME con el volumen Docker destino de n8n}"

N8N_CONTAINER="${N8N_CONTAINER:-n8n_local}"
RESTORE_CONFIRM="${RESTORE_CONFIRM:-}"

command -v pg_restore >/dev/null || { echo "ERROR: instala postgresql-client (pg_restore)." >&2; exit 1; }
command -v docker >/dev/null || { echo "ERROR: Docker no está disponible." >&2; exit 1; }

for required in postgres.dump n8n-data.tar.gz manifest.txt SHA256SUMS; do
  [[ -f "$BACKUP_DIR/$required" ]] || { echo "ERROR: falta $required en $BACKUP_DIR" >&2; exit 1; }
done

echo "Verificando integridad del respaldo..."
(cd "$BACKUP_DIR" && sha256sum --check SHA256SUMS)

docker volume inspect "$N8N_VOLUME" >/dev/null
if docker ps --format '{{.Names}}' | grep -Fxq "$N8N_CONTAINER"; then
  echo "ERROR: $N8N_CONTAINER está ejecutándose."
  echo "Detén n8n antes de restaurar: docker stop $N8N_CONTAINER"
  exit 1
fi

echo
echo "ATENCIÓN: se reemplazarán los objetos/datos de PostgreSQL del destino"
echo "y el contenido del volumen Docker '$N8N_VOLUME'."
echo "Base destino: se toma de DATABASE_URL (no se muestra por seguridad)."
echo "Respaldo: $BACKUP_DIR"
echo

if [[ "$RESTORE_CONFIRM" != "RESTAURAR" ]]; then
  read -r -p "Escribe RESTAURAR para continuar: " answer
  [[ "$answer" == "RESTAURAR" ]] || { echo "Restauración cancelada."; exit 1; }
else
  answer="$RESTORE_CONFIRM"
fi

echo "1/3 - Restaurando PostgreSQL..."
pg_restore --dbname="$DATABASE_URL" --clean --if-exists --no-owner --no-acl \
  --exit-on-error "$BACKUP_DIR/postgres.dump"

echo "2/3 - Restaurando el volumen de n8n..."
docker run --rm \
  -v "$N8N_VOLUME:/data" \
  -v "$BACKUP_DIR:/backup:ro" \
  alpine:3.20 \
  sh -ec 'find /data -mindepth 1 -maxdepth 1 -exec rm -rf {} +; tar xzf /backup/n8n-data.tar.gz -C /data'

echo "3/3 - Restauración completada."
echo "Inicia n8n cuando corresponda: docker start $N8N_CONTAINER"
echo "Verifica la aplicación, las credenciales/conexiones de n8n y las consultas de FinEdu-Bot."
