# FinEdu-Bot - Backup & Restore

## Objetivo

Implementar un mecanismo de respaldo y recuperación
para PostgreSQL y n8n.

## Componentes

- backup.sh: genera los respaldos.
- restore.sh: recupera los respaldos.
- backup-restore.yml: automatiza las pruebas.
- backup-restore.test.sh: valida los archivos y scripts.

## Requisitos

- Docker.
- PostgreSQL.
- pg_dump.
- pg_restore.
- Bash.
- GitHub Actions.

## Generar backup

Desde la raíz del proyecto:

    chmod +x scripts/backup-restore/*.sh

    ./scripts/backup-restore/backup.sh

## Restaurar backup

    ./scripts/backup-restore/restore.sh ./backups/FECHA

La restauración requiere confirmación.

## Verificación

El workflow comprueba:

1. Existencia de archivos.
2. Sintaxis Bash.
3. Protección de restauración.
4. Integración del workflow.
5. Protección de archivos privados.
6. Backup de PostgreSQL.
7. Backup del volumen n8n.
8. Integridad SHA-256.
9. Restauración de PostgreSQL.
10. Restauración del volumen n8n.

## Seguridad

Los respaldos pueden contener información sensible.

No subirlos al repositorio.

No publicar archivos .env.

La restauración debe probarse primero
en un entorno aislado.
