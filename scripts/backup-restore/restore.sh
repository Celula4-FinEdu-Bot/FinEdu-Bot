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

BACKUP_DIR="${1:-}"

if [[ -z "$BACKUP_DIR" ]]; then
    echo "Uso: ./scripts/backup-restore/restore.sh <ruta-backup>"
    exit 2
fi

BACKUP_DIR="$(cd -- "$BACKUP_DIR" && pwd)"

: "${DATABASE_URL:?Configura DATABASE_URL}"
: "${N8N_VOLUME:?Configura N8N_VOLUME}"

N8N_CONTAINER="${N8N_CONTAINER:-n8n_local}"

command -v pg_restore >/dev/null || {
    echo "ERROR: pg_restore no está instalado."
    exit 1
}

command -v docker >/dev/null || {
    echo "ERROR: Docker no está disponible."
    exit 1
}

for FILE in postgres.dump n8n-data.tar.gz manifest.txt SHA256SUMS; do
    if [[ ! -f "$BACKUP_DIR/$FILE" ]]; then
        echo "ERROR: falta $FILE"
        exit 1
    fi
done

echo "Verificando integridad..."

(
    cd "$BACKUP_DIR"
    sha256sum --check SHA256SUMS
)

docker volume inspect "$N8N_VOLUME" >/dev/null

if docker ps --format '{{.Names}}' | grep -Fxq "$N8N_CONTAINER"; then
    echo "ERROR: detén n8n antes de restaurar."
    exit 1
fi

echo ""
echo "ATENCIÓN:"
echo "Se reemplazarán los datos del destino PostgreSQL."
echo "Se reemplazará el contenido del volumen n8n."
echo ""

if [[ "${RESTORE_CONFIRM:-}" != "RESTAURAR" ]]; then
    read -r -p "Escribe RESTAURAR para continuar: " ANSWER

    if [[ "$ANSWER" != "RESTAURAR" ]]; then
        echo "Restauración cancelada."
        exit 1
    fi
fi

echo "1/2 - Restaurando PostgreSQL..."

pg_restore \
    --dbname="$DATABASE_URL" \
    --clean \
    --if-exists \
    --no-owner \
    --no-acl \
    --exit-on-error \
    "$BACKUP_DIR/postgres.dump"

echo "2/2 - Restaurando volumen n8n..."

docker run --rm \
    -v "$N8N_VOLUME:/data" \
    -v "$BACKUP_DIR:/backup:ro" \
    alpine:3.20 \
    sh -ec '
        find /data -mindepth 1 -maxdepth 1 -exec rm -rf {} +
        tar xzf /backup/n8n-data.tar.gz -C /data
    '

echo ""
echo "RESTAURACIÓN COMPLETADA."
echo "Inicia n8n cuando corresponda."