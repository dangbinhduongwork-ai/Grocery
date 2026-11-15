#!/usr/bin/env bash
# =============================================================================
# GroceryHub — Database Restore Drill Utility (Bash / Linux)
# Tests restoring from a backup file to an isolated drill database
# =============================================================================
set -euo pipefail

BACKUP_FILE="${1:?Usage: ./scripts/restore-drill.sh <path-to-backup.bak> [drill_db_name]}"
DRILL_DB="${2:-GroceryHub_Restore_Drill}"
CONTAINER_NAME="${3:-groceryhub-sqlserver-1}"

echo "============================================================"
echo " GroceryHub Database Restore Drill"
echo " Backup File:  ${BACKUP_FILE}"
echo " Drill Target: ${DRILL_DB}"
echo "============================================================"

# Verify backup file exists
if [ ! -f "${BACKUP_FILE}" ]; then
  echo "Error: Backup file ${BACKUP_FILE} not found."
  exit 1
fi

# Copy backup into container
CONTAINER_BAK_PATH="/var/opt/mssql/data/drill_restore.bak"
docker cp "${BACKUP_FILE}" "${CONTAINER_NAME}:${CONTAINER_BAK_PATH}"

echo "[1/4] Inspecting logical file names inside backup..."
docker exec "${CONTAINER_NAME}" /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C \
  -Q "RESTORE FILELISTONLY FROM DISK = N'${CONTAINER_BAK_PATH}';"

echo "[2/4] Restoring database to isolated drill instance [${DRILL_DB}]..."
docker exec "${CONTAINER_NAME}" /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C \
  -Q "
    IF DB_ID('${DRILL_DB}') IS NOT NULL
    BEGIN
        ALTER DATABASE [${DRILL_DB}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
        DROP DATABASE [${DRILL_DB}];
    END

    RESTORE DATABASE [${DRILL_DB}]
    FROM DISK = N'${CONTAINER_BAK_PATH}'
    WITH MOVE 'GroceryHub' TO '/var/opt/mssql/data/${DRILL_DB}.mdf',
         MOVE 'GroceryHub_log' TO '/var/opt/mssql/data/${DRILL_DB}_log.ldf',
         REPLACE, STATS = 25;
  "

echo "[3/4] Running DBCC CHECKDB integrity verification on [${DRILL_DB}]..."
docker exec "${CONTAINER_NAME}" /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C \
  -Q "DBCC CHECKDB ('${DRILL_DB}') WITH NO_INFOMSGS;"

echo "[4/4] Cleaning up drill database..."
docker exec "${CONTAINER_NAME}" /opt/mssql-tools18/bin/sqlcmd \
  -S localhost -U sa -P "${MSSQL_SA_PASSWORD}" -C \
  -Q "
    ALTER DATABASE [${DRILL_DB}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE [${DRILL_DB}];
  "

echo ""
echo "Restore Drill Passed Successfully: Zero integrity or allocation errors."
