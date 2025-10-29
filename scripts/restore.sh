#!/bin/sh
# PostgreSQL Restore Script for Meeting Management System
# This script restores a PostgreSQL database from a backup file

set -e

# Configuration
BACKUP_DIR="/backups"
DB_HOST="postgres"
DB_NAME="${POSTGRES_DB:-meetingmanagement}"
DB_USER="${POSTGRES_USER:-postgres}"

# Check if backup file is provided
if [ -z "$1" ]; then
    echo "Usage: $0 <backup_file>"
    echo ""
    echo "Available backups:"
    ls -lh "${BACKUP_DIR}"
    exit 1
fi

BACKUP_FILE="$1"

# Check if backup file exists
if [ ! -f "${BACKUP_FILE}" ]; then
    echo "Error: Backup file not found: ${BACKUP_FILE}"
    exit 1
fi

echo "Starting restore at $(date)"
echo "Database: ${DB_NAME}"
echo "Backup file: ${BACKUP_FILE}"

# Decompress if needed
if [[ "${BACKUP_FILE}" == *.gz ]]; then
    echo "Decompressing backup file..."
    gunzip -c "${BACKUP_FILE}" > /tmp/restore.sql
    RESTORE_FILE="/tmp/restore.sql"
else
    RESTORE_FILE="${BACKUP_FILE}"
fi

# Drop existing connections
echo "Terminating existing connections..."
psql -h "${DB_HOST}" -U "${DB_USER}" -d postgres -c "SELECT pg_terminate_backend(pid) FROM pg_stat_activity WHERE datname = '${DB_NAME}' AND pid <> pg_backend_pid();"

# Drop and recreate database
echo "Dropping and recreating database..."
psql -h "${DB_HOST}" -U "${DB_USER}" -d postgres -c "DROP DATABASE IF EXISTS ${DB_NAME};"
psql -h "${DB_HOST}" -U "${DB_USER}" -d postgres -c "CREATE DATABASE ${DB_NAME};"

# Restore the backup
echo "Restoring backup..."
pg_restore -h "${DB_HOST}" -U "${DB_USER}" -d "${DB_NAME}" -v "${RESTORE_FILE}"

# Clean up temporary file
if [ -f "/tmp/restore.sql" ]; then
    rm /tmp/restore.sql
fi

echo "Restore completed successfully at $(date)"
