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

: "${DATABASE_URL:?Configura DATABASE_URL en .env o en el entorno}"
BACKUP_ROOT="${BACKUP_ROOT:-$REPO_ROOT/backups}"
N8N_CONTAINER="${N8N_CONTAINER:-n8n_local}"
N8N_VOLUME="${N8N_VOLUME:-}"

command -v pg_dump >/dev/null || { echo "ERROR: instala postgresql-client (pg_dump)." >&2; exit 1; }
command -v docker >/dev/null || { echo "ERROR: Docker no está disponible." >&2; exit 1; }

if [[ -z "$N8N_VOLUME" ]]; then
  mapfile -t volumes < <(docker volume ls -q --filter label=com.docker.compose.volume=n8n_data)
  if [[ ${#volumes[@]} -eq 1 ]]; then
    N8N_VOLUME="${volumes[0]}"
  else
    echo "ERROR: define N8N_VOLUME con el nombre real del volumen Docker de n8n." >&2
    echo "Volúmenes encontrados: ${volumes[*]:-(ninguno)}" >&2
    exit 1
  fi
fi

docker volume inspect "$N8N_VOLUME" >/dev/null
timestamp="$(date -u +'%Y%m%dT%H%M%SZ')"
backup_dir="$BACKUP_ROOT/$timestamp"
mkdir -p "$backup_dir"
was_running=0

if docker ps --format '{{.Names}}' | grep -Fxq "$N8N_CONTAINER"; then
  was_running=1
  echo "Deteniendo temporalmente $N8N_CONTAINER para respaldar su volumen de forma consistente..."
  docker stop "$N8N_CONTAINER" >/dev/null
fi

restart_n8n() {
  if [[ "$was_running" -eq 1 ]]; then
    echo "Iniciando nuevamente $N8N_CONTAINER..."
    docker start "$N8N_CONTAINER" >/dev/null || true
  fi
}
trap restart_n8n EXIT

echo "1/4 - Exportando PostgreSQL..."
pg_dump --dbname="$DATABASE_URL" --format=custom --no-owner --no-acl \
  --file="$backup_dir/postgres.dump"

echo "2/4 - Respaldando el volumen persistente de n8n ($N8N_VOLUME)..."
docker run --rm \
  -v "$N8N_VOLUME:/source:ro" \
  -v "$backup_dir:/backup" \
  alpine:3.20 \
  sh -c 'tar czf /backup/n8n-data.tar.gz -C /source .'

echo "3/4 - Generando manifiesto y checksums..."
cat > "$backup_dir/manifest.txt" <<EOF
Proyecto=FinEdu-Bot
Fecha_UTC=$timestamp
PostgreSQL=postgres.dump
n8n_volume=$N8N_VOLUME
n8n_archive=n8n-data.tar.gz
Formato_PostgreSQL=pg_dump_custom
EOF
(
  cd "$backup_dir"
  sha256sum postgres.dump n8n-data.tar.gz manifest.txt > SHA256SUMS
)

echo "4/4 - Respaldo terminado."
echo "BACKUP_DIR=$backup_dir"
echo "Guarda esta carpeta fuera del equipo y no la publiques en Git."
