#!/usr/bin/env bash
set -Eeuo pipefail

ROOT="$(cd -- "$(dirname -- "${BASH_SOURCE[0]}")/../.." && pwd)"
BACKUP_DIR="$ROOT/scripts/backup-restore"
WORKFLOW="$ROOT/.github/workflows/backup-restore.yml"

echo "TEST 1/5 - Existen los scripts y archivos requeridos"
for file in backup.sh restore.sh .env.example README.md; do
  test -s "$BACKUP_DIR/$file"
  echo "  ✓ $file"
done

echo "TEST 2/5 - Sintaxis Bash"
bash -n "$BACKUP_DIR/backup.sh"
bash -n "$BACKUP_DIR/restore.sh"
echo "  ✓ backup.sh y restore.sh"

echo "TEST 3/5 - Protección de restauración"
grep -q 'RESTORE_CONFIRM' "$BACKUP_DIR/restore.sh"
grep -q 'sha256sum --check SHA256SUMS' "$BACKUP_DIR/restore.sh"
grep -q 'docker ps' "$BACKUP_DIR/restore.sh"
echo "  ✓ confirmación manual, checksums y control del contenedor"

echo "TEST 4/5 - El workflow ejecuta la prueba de integración"
grep -q 'backup-restore.test.sh' "$WORKFLOW"
grep -q 'Generar respaldo de demostración' "$WORKFLOW"
grep -q 'Restaurar en base y volumen de prueba' "$WORKFLOW"
grep -q 'Verificar datos PostgreSQL y volumen n8n' "$WORKFLOW"
echo "  ✓ backup, restore y verificación están conectados al workflow"

echo "TEST 5/5 - No se versionan respaldos ni secretos locales"
grep -q 'backups/' "$ROOT/.gitignore" || grep -q 'backups/' "$BACKUP_DIR/.gitignore"
grep -q '^DATABASE_URL=' "$BACKUP_DIR/.env.example"
echo "  ✓ configuración de conexión documentada; .env local no se incluye"

echo "RESULTADO: 5/5 pruebas locales aprobadas."
