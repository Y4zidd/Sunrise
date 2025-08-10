#!/bin/bash

# Backup Observatory data ke Sunrise repo
SUNRISE_REMOTE="sunrise-backup"
SUNRISE_REPO="https://github.com/Y4zidd/Sunrise.git"
BACKUP_BRANCH="observatory-backup-$(date +%Y%m%d)"

echo "🚀 Backup Observatory ke Sunrise repo..."

# Setup remote
git remote add "$SUNRISE_REMOTE" "$SUNRISE_REPO" 2>/dev/null
git fetch "$SUNRISE_REMOTE"

# Buat branch
git checkout -b "$BACKUP_BRANCH" "$SUNRISE_REMOTE/master" 2>/dev/null || git checkout -b "$BACKUP_BRANCH"

# Copy Observatory data
mkdir -p observatory-backup
cp -r /home/ubuntu/Observatory/data/backups/custom-beatmap-status/ observatory-backup/
cp /home/ubuntu/Observatory/backup-restore-custom-beatmap.sh observatory-backup/

# Commit & push
git add observatory-backup/
git commit -m "Add Observatory custom beatmap backup - $(date)"
git push "$SUNRISE_REMOTE" "$BACKUP_BRANCH"

# Kembali ke master
git checkout master

echo "✅ Backup selesai! Branch: $BACKUP_BRANCH"
