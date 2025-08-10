#!/bin/bash

# =====================================================
# SCRIPT BACKUP KE SUNRISE REPOSITORY
# =====================================================
# Script untuk backup data Observatory dan Sunrise ke branch Sunrise repo

SUNRISE_REMOTE="sunrise-backup"
SUNRISE_REPO="https://github.com/Y4zidd/Sunrise.git"
BACKUP_BRANCH="data-backup-$(date +%Y%m%d)"
BACKUP_DIR="sunrise-backups"

echo "====================================================="
echo "BACKUP KE SUNRISE REPOSITORY"
echo "====================================================="

# Cek apakah sudah ada remote Sunrise
if ! git remote | grep -q "$SUNRISE_REMOTE"; then
    echo "📡 Menambahkan remote Sunrise..."
    git remote add "$SUNRISE_REMOTE" "$SUNRISE_REPO"
else
    echo "✅ Remote Sunrise sudah ada"
fi

# Fetch latest dari Sunrise
echo "📥 Fetch latest dari Sunrise repository..."
git fetch "$SUNRISE_REMOTE"

# Buat branch backup baru
echo "🌿 Membuat branch backup: $BACKUP_BRANCH"
git checkout -b "$BACKUP_BRANCH" "$SUNRISE_REMOTE/master" 2>/dev/null || git checkout -b "$BACKUP_BRANCH"

# Buat folder backup
echo "📁 Membuat folder backup..."
mkdir -p "$BACKUP_DIR"

# Copy data Observatory (jika ada)
if [ -d "/home/ubuntu/Observatory/data/backups/custom-beatmap-status" ]; then
    echo "📋 Copy data Observatory..."
    cp -r /home/ubuntu/Observatory/data/backups/custom-beatmap-status/ "$BACKUP_DIR/observatory-beatmap-backup/"
fi

# Copy data Sunrise (hanya folder Backups yang penting)
echo "📋 Copy data Sunrise (Backups)..."
if [ -d "/home/ubuntu/Sunrise/Data/Backups" ]; then
    cp -r /home/ubuntu/Sunrise/Data/Backups/ "$BACKUP_DIR/sunrise-backups/"
fi

# Copy file penting lainnya dari Sunrise Data
echo "📋 Copy file penting Sunrise..."
cp /home/ubuntu/Sunrise/Data/banned-usernames.txt "$BACKUP_DIR/" 2>/dev/null || echo "File banned-usernames.txt tidak ada"
cp -r /home/ubuntu/Sunrise/Data/scripts/ "$BACKUP_DIR/sunrise-scripts/" 2>/dev/null || echo "Folder scripts tidak ada"

# Copy script backup
echo "📋 Copy script backup..."
cp /home/ubuntu/Observatory/backup-restore-custom-beatmap.sh "$BACKUP_DIR/"
cp /home/ubuntu/Sunrise/Data/scripts/backup-to-sunrise-repo.sh "$BACKUP_DIR/"

# Buat README untuk branch backup
cat > "$BACKUP_DIR/README.md" << 'EOF'
# 📁 Data Backup Repository

Branch ini berisi backup data dari:
- **Observatory**: Custom beatmap status backup
- **Sunrise**: Data files dan konfigurasi

## 📊 Struktur Data

```
sunrise-backups/
├── observatory-beatmap-backup/     # Backup custom beatmap status
├── sunrise-data/                   # Data files dari Sunrise
├── backup-restore-custom-beatmap.sh
├── backup-to-sunrise-repo.sh
└── README.md
```

## 🔄 Cara Restore

### Observatory Custom Beatmap
```bash
cd observatory-beatmap-backup
./backup-restore-custom-beatmap.sh restore
```

### Sunrise Data
```bash
# Copy data ke folder Sunrise yang sesuai
cp -r sunrise-data/* /path/to/sunrise/Data/
```

---
*Backup dibuat: $(date)*
EOF

# Commit semua perubahan
echo "💾 Commit perubahan..."
git add "$BACKUP_DIR/"
git commit -m "Add comprehensive backup data from Observatory and Sunrise

- Observatory custom beatmap status backup
- Sunrise data files
- Backup and restore scripts
- Documentation

Backup created: $(date)"

# Push ke branch backup
echo "🚀 Push ke branch backup: $BACKUP_BRANCH"
git push "$SUNRISE_REMOTE" "$BACKUP_BRANCH"

# Kembali ke branch master
git checkout master

echo ""
echo "✅ Backup berhasil!"
echo "📁 Branch: $BACKUP_BRANCH"
echo "🌐 URL: https://github.com/Y4zidd/Sunrise/tree/$BACKUP_BRANCH"
echo ""
echo "📝 Catatan:"
echo "- Branch ini tidak akan konflik dengan Observatory remote"
echo "- Data backup tersimpan aman di repository Sunrise"
echo "- Bisa di-restore kapan saja dari branch ini"
