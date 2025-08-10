# 📁 Custom Beatmap Status Backup

Folder ini berisi backup data custom beatmap status dari server Sunrise.

## 📊 Struktur File

- **`custom_beatmap_ids_restore.sql`** - File SQL untuk restore ke Observatory
- **`README.md`** - Dokumentasi ini

## 🔄 Cara Penggunaan

### Backup
```bash
cd /home/ubuntu/Observatory
./backup-restore-custom-beatmap.sh backup
```

### Restore
```bash
cd /home/ubuntu/Observatory
./backup-restore-custom-beatmap.sh restore
```

## 📋 Format Data

Setiap baris dalam file backup berformat:
```
BeatmapSetId:BeatmapHash:Status
```

Contoh:
```
43776:49dfb4686a1ee7dae4b639e4ccbc1c1b:1
150945:f8a383c1de40613c61aa86f0fcec60f2:1
```

## 🗄️ Status Mapping

- **1** = Ranked
- **2** = Approved  
- **3** = Qualified
- **4** = Loved

## 📅 Backup History

Backup dibuat dengan timestamp untuk tracking history:
- `custom_beatmap_ids_backup_20240810_071800.txt` - Backup tanggal 10 Agustus 2024 jam 07:18

## 🚀 Restore ke Observatory

1. Copy file restore SQL ke server Observatory
2. Jalankan: `psql -U username -d database < custom_beatmap_ids_restore.sql`
3. Atau copy isi file ke database Observatory secara manual

---
*Last updated: $(date)*
