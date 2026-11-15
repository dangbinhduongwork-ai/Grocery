#!/usr/bin/env bash
# =============================================================================
# GroceryHub — Database Backup Utility (Bash / Linux)
# Backs up SQL Server database, validates CHECKSUM, and computes SHA-256 hash
# =============================================================================
set -euo pipefail

DB_NAME="${1:-GroceryHub}"
BACKUP_DIR="${2:-./backups}"
CONTAINER_NAME="${3:-groceryhub-sqlserver-1}"
TIMESTAMP=$(date +"%Y%m%d-%H%M%S")
BACKUP_FILE="${DB_NAME}_Full_${TIMESTAMP}.bak"

echo "============================================================"
echo " GroceryHub Linux/Container Database Backup"
echo " Database:  ${DB_NAME}"
echo " Container: ${CONTAINER_NAME}"
echo " File:      ${BACKUP_FILE}"
echo "============================================================"

mkdir -p "${BACKUP_DIR}"

# Run backup inside SQL Server container
docker exec "${CONTAINER_NAME}" /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C \
  -Q "BACKUP DATABASE [${DB_NAME}] TO DISK = N'/var/opt/mssql/data/${BACKUP_FILE}' WITH FORMAT, INIT, COMPRESSION, CHECKSUM, STATS = 10; RESTORE VERIFYONLY FROM DISK = N'/var/opt/mssql/data/${BACKUP_FILE}' WITH CHECKSUM;"

# Copy backup out of container to host backup directory
docker cp "${CONTAINER_NAME}:/var/opt/mssql/data/${BACKUP_FILE}" "${BACKUP_DIR}/${BACKUP_FILE}"

# Calculate SHA256 checksum
SHA256_HASH=$(sha256sum "${BACKUP_DIR}/${BACKUP_FILE}" | awk '{print $1}')
FILE_SIZE=$(du -h "${BACKUP_DIR}/${BACKUP_FILE}" | awk '{print $1}')

echo ""
echo "Backup Completed Successfully!"
echo "  Saved to: ${BACKUP_DIR}/${BACKUP_FILE}"
echo "  Size:     ${FILE_SIZE}"
echo "  SHA256:   ${SHA256_HASH}"
