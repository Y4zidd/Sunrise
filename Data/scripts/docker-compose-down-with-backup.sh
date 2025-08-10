#!/bin/bash

# Script wrapper untuk docker compose down dengan backup otomatis
# Penggunaan: ./docker-compose-down-with-backup.sh

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKUP_SCRIPT="$SCRIPT_DIR/auto_backup_mysql.sh"

echo "🚀 Starting docker compose down with automatic backup..."

# Jalankan backup terlebih dahulu
if [ -f "$BACKUP_SCRIPT" ]; then
    echo "📦 Running MySQL backup before shutdown..."
    bash "$BACKUP_SCRIPT"
    
    if [ $? -eq 0 ]; then
        echo "✅ Backup completed successfully"
    else
        echo "❌ Backup failed! Do you want to continue with shutdown? (y/N)"
        read -r response
        if [[ ! "$response" =~ ^[Yy]$ ]]; then
            echo "🛑 Shutdown cancelled"
            exit 1
        fi
    fi
else
    echo "⚠️  Backup script not found: $BACKUP_SCRIPT"
    echo "🔄 Continuing with shutdown without backup..."
fi

# Jalankan docker compose down
echo "🔄 Running docker compose down..."
docker compose down

echo "✅ Docker compose down completed!" 