#!/usr/bin/env bash

set -Eeuo pipefail

SCRIPT_DIR="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd -- "$SCRIPT_DIR/../.." && pwd)"

ENV_FILE="${ENV_FILE:-$REPO_ROOT/.env}"

if [[ -f "$ENV_FILE" ]]; then
    set -a
    source "$ENV_FILE"
    set +a
fi

: "${DATABASE_URL:?Configura DATABASE_URL}"

BACKUP_ROOT="${BACKUP_ROOT:-$REPO_ROOT/backups}"
N8N_CONTAINER="${N8N_CONTAINER:-n8n_local}"
N8N_VOLUME="${N8N_VOLUME:-}"

command -v pg_dump >/dev/null || {
    echo "ERROR: pg_dump no está instalado."
    exit 1
}

command -v docker >/dev/null || {
    echo "ERROR: Docker no está disponible."
    exit 1
}

if [[ -z "$N8N_VOLUME" ]]; then
    mapfile -t volumes < <(
        docker volume ls -q \
        --filter label=com.docker.compose.volume=n8n_data
    )

    if [[ ${#volumes[@]} -eq 1 ]]; then
        N8N_VOLUME="${volumes[0]}"
    else
        echo "ERROR: configura N8N_VOLUME."
        exit 1
    fi
fi

docker volume inspect "$N8N_VOLUME" >/dev/null

TIMESTAMP="$(date -u +'%Y%m%dT%H%M%SZ')"
BACKUP_DIR="$BACKUP_ROOT/$TIMESTAMP"

mkdir -p "$BACKUP_DIR"

WAS_RUNNING=0

if docker ps --format '{{.Names}}' | grep -Fxq "$N8N_CONTAINER"; then
    WAS_RUNNING=1

    echo "Deteniendo temporalmente n8n..."
    docker stop "$N8N_CONTAINER" >/dev/null
fi

restart_n8n() {
    if [[ "$WAS_RUNNING" -eq 1 ]]; then
        echo "Iniciando nuevamente n8n..."
        docker start "$N8N_CONTAINER" >/dev/null || true
    fi
}

trap restart_n8n EXIT

echo "1/4 - Respaldando PostgreSQL..."

pg_dump \
    --dbname="$DATABASE_URL" \
    --format=custom \
    --no-owner \
    --no-acl \
    --file="$BACKUP_DIR/postgres.dump"

echo "2/4 - Respaldando volumen n8n..."

docker run --rm \
    -v "$N8N_VOLUME:/source:ro" \
    -v "$BACKUP_DIR:/backup" \
    alpine:3.20 \
    sh -c 'tar czf /backup/n8n-data.tar.gz -C /source .'

echo "3/4 - Generando manifiesto..."

cat > "$BACKUP_DIR/manifest.txt" <<EOF
Proyecto=FinEdu-Bot
Fecha_UTC=$TIMESTAMP
PostgreSQL=postgres.dump
n8n_volume=$N8N_VOLUME
n8n_archive=n8n-data.tar.gz
Formato_PostgreSQL=pg_dump_custom
EOF

(
    cd "$BACKUP_DIR"

    sha256sum \
        postgres.dump \
        n8n-data.tar.gz \
        manifest.txt \
        > SHA256SUMS
)

echo "4/4 - Backup completado."

echo "BACKUP_DIR=$BACKUP_DIR"