#!/bin/bash
# =============================================================================
# VibeXLearn Platform - Restore Script
# =============================================================================
# Kullanım: ./restore.sh [backup_file]
# Örnek: ./restore.sh postgres_20240101_020000.dump
# =============================================================================

set -e

# Renkler
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

log_info() { echo -e "[INFO] $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Konfigürasyon
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(dirname "$SCRIPT_DIR")"
BACKUP_DIR="/var/backups/vibexlearn"

# Environment yükle
if [ -f "$DEPLOY_DIR/.env.production" ]; then
    set -a
    source "$DEPLOY_DIR/.env.production"
    set +a
fi

# Backup dosyası kontrol
BACKUP_FILE=${1:-}

if [ -z "$BACKUP_FILE" ]; then
    log_info "Mevcut backup'lar:"
    ls -lh "$BACKUP_DIR"/*.dump 2>/dev/null || echo "Backup bulunamadı"
    echo ""
    read -p "Restore edilecek backup dosyasını girin: " BACKUP_FILE
fi

BACKUP_PATH="$BACKUP_DIR/$BACKUP_FILE"

if [ ! -f "$BACKUP_PATH" ]; then
    log_error "Backup dosyası bulunamadı: $BACKUP_PATH"
    exit 1
fi

# Onay al
log_warn "DİKKAT: Bu işlem mevcut veritabanını SİLECEK!"
log_warn "Backup: $BACKUP_FILE"
read -p "Devam etmek istiyor musunuz? (yes/no): " confirm

if [ "$confirm" != "yes" ]; then
    log_info "Restore iptal edildi"
    exit 0
fi

# Servisleri durdur
log_info "API servisi durduruluyor..."
docker-compose -f "$DEPLOY_DIR/docker-compose.prod.yml" stop api

# PostgreSQL Restore
if [[ "$BACKUP_FILE" == postgres*.dump ]]; then
    log_info "PostgreSQL restore yapılıyor..."

    # Mevcut veritabanını temizle
    docker exec -i vibexlearn-postgres-prod psql -U "$POSTGRES_USER" -d postgres \
        -c "SELECT pg_terminate_backend(pg_stat_activity.pid) FROM pg_stat_activity WHERE pg_stat_activity.datname = '$POSTGRES_DB' AND pid <> pg_backend_pid();"

    docker exec -i vibexlearn-postgres-prod psql -U "$POSTGRES_USER" -d postgres \
        -c "DROP DATABASE IF EXISTS $POSTGRES_DB;"

    docker exec -i vibexlearn-postgres-prod psql -U "$POSTGRES_USER" -d postgres \
        -c "CREATE DATABASE $POSTGRES_DB;"

    # Backup'ı restore et
    cat "$BACKUP_PATH" | docker exec -i vibexlearn-postgres-prod pg_restore \
        -U "$POSTGRES_USER" \
        -d "$POSTGRES_DB" \
        --no-owner \
        --no-privileges \
        --verbose

    log_success "PostgreSQL restore tamamlandı"

# Redis Restore
elif [[ "$BACKUP_FILE" == redis*.rdb ]]; then
    log_info "Redis restore yapılıyor..."

    # Redis'i durdur
    docker stop vibexlearn-redis-prod

    # RDB dosyasını kopyala
    docker cp "$BACKUP_PATH" vibexlearn-redis-prod:/data/dump.rdb

    # Redis'i başlat
    docker start vibexlearn-redis-prod

    log_success "Redis restore tamamlandı"

else
    log_error "Bilinmeyen backup formatı: $BACKUP_FILE"
    exit 1
fi

# Servisleri başlat
log_info "Servisler başlatılıyor..."
docker-compose -f "$DEPLOY_DIR/docker-compose.prod.yml" start api

# Health check
log_info "Health check yapılıyor..."
sleep 10

for i in {1..30}; do
    if docker exec vibexlearn-api-prod curl -sf http://localhost:8080/health > /dev/null 2>&1; then
        log_success "API sağlıklı!"
        break
    fi
    if [ $i -eq 30 ]; then
        log_error "API health check başarısız!"
        exit 1
    fi
    sleep 5
done

log_success "============================================"
log_success "Restore başarıyla tamamlandı!"
log_success "============================================"
