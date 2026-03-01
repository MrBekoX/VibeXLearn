#!/bin/bash
# =============================================================================
# VibeXLearn Platform - Backup Script
# =============================================================================
# Kullanım: ./backup.sh
# Cron: 0 2 * * * /path/to/backup.sh (her gün saat 02:00)
# =============================================================================

set -e

# Renkler
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() { echo -e "[INFO] $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Konfigürasyon
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(dirname "$SCRIPT_DIR")"
BACKUP_DIR="/var/backups/vibexlearn"
DATE=$(date +%Y%m%d_%H%M%S)
RETENTION_DAYS=${BACKUP_RETENTION_DAYS:-7}

# Environment yükle
if [ -f "$DEPLOY_DIR/.env.production" ]; then
    set -a
    source "$DEPLOY_DIR/.env.production"
    set +a
fi

# Backup klasörünü oluştur
mkdir -p "$BACKUP_DIR"

log_info "Backup başlatılıyor: $DATE"

# ═══════════════════════════════════════════════════════════════════════════════
# PostgreSQL Backup
# ═══════════════════════════════════════════════════════════════════════════════
log_info "PostgreSQL backup alınıyor..."

docker exec vibexlearn-postgres-prod pg_dump \
    -U "$POSTGRES_USER" \
    -d "$POSTGRES_DB" \
    --format=custom \
    --compress=9 \
    > "$BACKUP_DIR/postgres_${DATE}.dump"

log_success "PostgreSQL backup tamamlandı: postgres_${DATE}.dump"

# ═══════════════════════════════════════════════════════════════════════════════
# Redis Backup
# ═══════════════════════════════════════════════════════════════════════════════
log_info "Redis backup alınıyor..."

docker exec vibexlearn-redis-prod redis-cli \
    -a "$REDIS_PASSWORD" \
    BGSAVE

# RDB dosyasının güncellenmesini bekle
sleep 5

docker cp vibexlearn-redis-prod:/data/dump.rdb "$BACKUP_DIR/redis_${DATE}.rdb"

log_success "Redis backup tamamlandı: redis_${DATE}.rdb"

# ═══════════════════════════════════════════════════════════════════════════════
# Elasticsearch Backup (Snapshot)
# ═══════════════════════════════════════════════════════════════════════════════
log_info "Elasticsearch snapshot alınıyor..."

# Snapshot repository kaydet (ilk seferde)
curl -s -X PUT "http://localhost:9200/_snapshot/vibexlearn_backup" \
    -H "Content-Type: application/json" \
    -u "elastic:$ELASTIC_PASSWORD" \
    -d '{
        "type": "fs",
        "settings": {
            "location": "/backups"
        }
    }' || true

# Snapshot al
curl -s -X PUT "http://localhost:9200/_snapshot/vibexlearn_backup/snapshot_${DATE}?wait_for_completion=true" \
    -u "elastic:$ELASTIC_PASSWORD"

log_success "Elasticsearch snapshot tamamlandı: snapshot_${DATE}"

# ═══════════════════════════════════════════════════════════════════════════════
# Eski Backup'ları Temizle
# ═══════════════════════════════════════════════════════════════════════════════
log_info "$RETENTION_DAYS günden eski backup'lar temizleniyor..."

find "$BACKUP_DIR" -name "*.dump" -mtime +$RETENTION_DAYS -delete
find "$BACKUP_DIR" -name "*.rdb" -mtime +$RETENTION_DAYS -delete
find "$BACKUP_DIR" -name "*.sql" -mtime +$RETENTION_DAYS -delete

log_success "Backup temizleme tamamlandı"

# ═══════════════════════════════════════════════════════════════════════════════
# Backup Özeti
# ═══════════════════════════════════════════════════════════════════════════════
log_success "============================================"
log_success "Backup tamamlandı!"
log_success "Tarih: $DATE"
log_success "Konum: $BACKUP_DIR"
log_success "============================================"

# Disk kullanımı
echo ""
echo "Backup disk kullanımı:"
du -sh "$BACKUP_DIR"

# Backup dosyaları
echo ""
echo "Mevcut backup'lar:"
ls -lh "$BACKUP_DIR"
