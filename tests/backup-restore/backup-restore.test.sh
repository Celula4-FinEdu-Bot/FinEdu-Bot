#!/usr/bin/env bash

set -Eeuo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"

BACKUP_DIR="$ROOT/scripts/backup-restore"
WORKFLOW="$ROOT/.github/workflows/backup-restore.yml"

echo "======================================"
echo " FINEDU-BOT - BACKUP & RESTORE TEST"
echo "======================================"

echo ""
echo "TEST 1/5 - Archivos requeridos"

for file in backup.sh restore.sh .env.example README.md; do
    test -s "$BACKUP_DIR/$file"
    echo "  ✓ $file"
done

echo ""
echo "TEST 2/5 - Sintaxis Bash"

bash -n "$BACKUP_DIR/backup.sh"
bash -n "$BACKUP_DIR/restore.sh"

echo "  ✓ Scripts correctos"

echo ""
echo "TEST 3/5 - Protección de restauración"

grep -q 'RESTORE_CONFIRM' "$BACKUP_DIR/restore.sh"
grep -q 'sha256sum --check SHA256SUMS' "$BACKUP_DIR/restore.sh"
grep -q 'docker ps' "$BACKUP_DIR/restore.sh"

echo "  ✓ Confirmación manual"
echo "  ✓ Integridad SHA-256"
echo "  ✓ Control del contenedor"

echo ""
echo "TEST 4/5 - Integración del workflow"

grep -q 'backup-restore.test.sh' "$WORKFLOW"
grep -q 'Generar respaldo' "$WORKFLOW"
grep -q 'Restaurar respaldo' "$WORKFLOW"
grep -q 'Verificar PostgreSQL y n8n' "$WORKFLOW"

echo "  ✓ Backup conectado"
echo "  ✓ Restore conectado"
echo "  ✓ Verificación conectada"

echo ""
echo "TEST 5/5 - Protección de archivos privados"

grep -q '^\*' "$BACKUP_DIR/.gitignore"
grep -q '^!.env.example$' "$BACKUP_DIR/.gitignore"
grep -q '^DATABASE_URL=' "$BACKUP_DIR/.env.example"
grep -qi 'no subirlos al repositorio' "$BACKUP_DIR/README.md"

echo "  ✓ .env local excluido"
echo "  ✓ Plantilla de configuración disponible"
echo "  ✓ Política de respaldos documentada"

echo ""
echo "======================================"
echo " RESULTADO: 5/5 PRUEBAS APROBADAS"
echo "======================================"