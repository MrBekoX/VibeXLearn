#!/bin/bash
# =============================================================================
# VibeXLearn Platform - Rollback Script
# =============================================================================
# Kullanım: ./rollback.sh [version]
# Örnek: ./rollback.sh 1.0.4
# =============================================================================

set -e

# Renkler
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m'

log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Konfigürasyon
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(dirname "$SCRIPT_DIR")"
COMPOSE_FILE="$DEPLOY_DIR/docker-compose.prod.yml"
ENV_FILE="$DEPLOY_DIR/.env.production"
DEPLOY_INFO="$DEPLOY_DIR/.deploy_info"

# Environment yükle
if [ -f "$ENV_FILE" ]; then
    set -a
    source "$ENV_FILE"
    set +a
fi

# Mevcut versiyonu kontrol et
if [ -f "$DEPLOY_INFO" ]; then
    CURRENT_VERSION=$(grep "DEPLOYED_VERSION" "$DEPLOY_INFO" | cut -d'=' -f2)
    log_info "Mevcut versiyon: $CURRENT_VERSION"
fi

# Rollback versiyonu
ROLLBACK_VERSION=${1:-}
if [ -z "$ROLLBACK_VERSION" ]; then
    log_error "Rollback versiyonu belirtmelisiniz!"
    log_info "Kullanım: ./rollback.sh <version>"
    exit 1
fi

log_warn "Rollback yapılıyor: $ROLLBACK_VERSION"
read -p "Emin misiniz? (y/N) " -n 1 -r
echo
if [[ ! $REPLY =~ ^[Yy]$ ]]; then
    log_info "Rollback iptal edildi"
    exit 0
fi

# API'yi belirtilen versiyona geri al
log_info "API $ROLLBACK_VERSION versiyonuna geri alınıyor..."
export API_VERSION=$ROLLBACK_VERSION

# Önce mevcut container'ları durdur
docker-compose -f "$COMPOSE_FILE" stop api
docker-compose -f "$COMPOSE_FILE" rm -f api

# Eski versiyonu başlat
docker-compose -f "$COMPOSE_FILE" up -d --no-deps --scale api=2 api

# Health check
log_info "API health check yapılıyor..."
for i in {1..30}; do
    if docker-compose -f "$COMPOSE_FILE" exec -T api curl -sf http://localhost:8080/health > /dev/null 2>&1; then
        log_success "API rollback başarılı!"
        break
    fi
    if [ $i -eq 30 ]; then
        log_error "API rollback başarısız!"
        exit 1
    fi
    sleep 5
done

# Nginx'i reload et
docker-compose -f "$COMPOSE_FILE" exec nginx nginx -s reload

# Deployment bilgisini güncelle
echo "DEPLOYED_VERSION=$ROLLBACK_VERSION" > "$DEPLOY_INFO"
echo "DEPLOYED_AT=$(date -u +"%Y-%m-%dT%H:%M:%SZ")" >> "$DEPLOY_INFO"
echo "ROLLBACK_FROM=$CURRENT_VERSION" >> "$DEPLOY_INFO"

log_success "============================================"
log_success "Rollback başarıyla tamamlandı!"
log_success "Version: $ROLLBACK_VERSION"
log_success "============================================"
