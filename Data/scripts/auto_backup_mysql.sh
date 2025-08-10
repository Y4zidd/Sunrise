#!/bin/bash

# Script backup otomatis MySQL saat docker compose down
# Dijalankan sebelum container di-stop

BACKUP_DIR="/home/ubuntu/Sunrise/Data/Backups/mysql"
TIMESTAMP=$(date +"%Y%m%d_%H%M%S")
BACKUP_FILE="mysql_backup_${TIMESTAMP}.sql"

# Buat direktori backup jika belum ada
mkdir -p "$BACKUP_DIR"

echo "🔄 Creating MySQL backup before shutdown..."
echo "📁 Backup location: $BACKUP_DIR/$BACKUP_FILE"

# Backup database MySQL
docker exec osu-sunrise-mysql-sunrise-db-1 mysqldump -u root -proot sunrise > "$BACKUP_DIR/$BACKUP_FILE"

# Periksa apakah backup berhasil
if [ $? -eq 0 ]; then
    echo "✅ MySQL backup completed successfully!"
    echo "📊 Backup size: $(du -h "$BACKUP_DIR/$BACKUP_FILE" | cut -f1)"
    
    # Hapus backup lama (simpan hanya 20 backup terbaru)
    cd "$BACKUP_DIR"
    ls -t mysql_backup_*.sql | tail -n +21 | xargs -r rm -f
    echo "🧹 Cleaned up old backups (kept 20 latest)"
else
    echo "❌ MySQL backup failed!"
    exit 1
fi 