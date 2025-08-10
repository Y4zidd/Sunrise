#!/bin/bash

# Script untuk restore database MySQL dari backup
# Penggunaan: ./restore_mysql_backup.sh [backup_file]

BACKUP_DIR="/home/ubuntu/Sunrise/Data/Backups/mysql"

if [ -z "$1" ]; then
    echo "📋 Available MySQL backups:"
    echo "=========================="
    ls -la "$BACKUP_DIR"/*.sql 2>/dev/null | awk '{print $9}' | sed 's|.*/||' | nl
    
    echo ""
    echo "❌ Please specify backup file to restore"
    echo "Usage: $0 <backup_file>"
    echo "Example: $0 mysql_backup_20250805_164257.sql"
    exit 1
fi

BACKUP_FILE="$1"
FULL_PATH="$BACKUP_DIR/$BACKUP_FILE"

# Periksa apakah file backup ada
if [ ! -f "$FULL_PATH" ]; then
    echo "❌ Backup file not found: $FULL_PATH"
    echo "📋 Available backups:"
    ls -la "$BACKUP_DIR"/*.sql 2>/dev/null | awk '{print $9}' | sed 's|.*/||'
    exit 1
fi

echo "🔄 Starting MySQL restore from: $BACKUP_FILE"
echo "📊 Backup size: $(du -h "$FULL_PATH" | cut -f1)"

# Periksa apakah container MySQL berjalan
if ! docker ps | grep -q "osu-sunrise-mysql-sunrise-db-1"; then
    echo "❌ MySQL container is not running!"
    echo "🔄 Starting MySQL container..."
    docker compose up mysql-sunrise-db -d
    sleep 10
fi

# Drop dan recreate database
echo "🗑️  Dropping existing database..."
docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "DROP DATABASE IF EXISTS sunrise; CREATE DATABASE sunrise;"

# Restore database
echo "📦 Restoring database from backup..."
docker exec -i osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot sunrise < "$FULL_PATH"

if [ $? -eq 0 ]; then
    echo "✅ MySQL restore completed successfully!"
    
    # Verifikasi data
    echo "🔍 Verifying restored data..."
    USER_COUNT=$(docker exec osu-sunrise-mysql-sunrise-db-1 mysql -u root -proot -e "USE sunrise; SELECT COUNT(*) FROM user;" -s -N)
    echo "👥 Users restored: $USER_COUNT"
    
    echo "🎉 Database restore completed! You can now start Sunrise services."
else
    echo "❌ MySQL restore failed!"
    exit 1
fi 