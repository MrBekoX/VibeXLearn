#!/bin/bash
# =============================================================================
# VibeXLearn Platform - Deployment Script
# =============================================================================
# Kullanım: ./deploy.sh [version]
# Örnek: ./deploy.sh 1.0.5
# =============================================================================

set -e

# Renkler
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
BLUE='\033[0;34m'
NC='\033[0m' # No Color

# Log fonksiyonu
log_info() { echo -e "${BLUE}[INFO]${NC} $1"; }
log_success() { echo -e "${GREEN}[SUCCESS]${NC} $1"; }
log_warn() { echo -e "${YELLOW}[WARN]${NC} $1"; }
log_error() { echo -e "${RED}[ERROR]${NC} $1"; }

# Konfigürasyon
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(dirname "$SCRIPT_DIR")"
COMPOSE_FILE="$DEPLOY_DIR/docker-compose.prod.yml"
ENV_FILE="$DEPLOY_DIR/.env.production"

# Version kontrol
VERSION=${1:-latest}
log_info "Deployment version: $VERSION"

# Environment dosyası kontrol
if [ ! -f "$ENV_FILE" ]; then
    log_error ".env.production dosyası bulunamadı!"
    log_info "Örnek dosyayı kopyalayıp düzenleyin: cp .env.production.example .env.production"
    exit 1
fi

# .env dosyasını yükle
set -a
source "$ENV_FILE"
set +a

# Gerekli değişkenlerin kontrolü
REQUIRED_VARS=(
    "POSTGRES_PASSWORD"
    "REDIS_PASSWORD"
    "ELASTIC_PASSWORD"
    "JWT_SECRET"
)

for var in "${REQUIRED_VARS[@]}"; do
    if [ -z "${!var}" ] || [[ "${!var}" == *"CHANGE_ME"* ]]; then
        log_error "$var değişkeni tanımlanmamış veya placeholder değerinde!"
        exit 1
    fi
done

log_success "Environment doğrulandı"

# Docker registry login (eğer private registry kullanılıyorsa)
if [ -n "$DOCKER_REGISTRY" ]; then
    log_info "Docker registry'ye bağlanılıyor: $DOCKER_REGISTRY"
    echo "$DOCKER_PASSWORD" | docker login "$DOCKER_REGISTRY" -u "$DOCKER_USER" --password-stdin || true
fi

# Eski container'ların sağlığını kontrol et
log_info "Mevcut deployment kontrol ediliyor..."
CURRENT_CONTAINERS=$(docker-compose -f "$COMPOSE_FILE" ps -q 2>/dev/null || true)

if [ -n "$CURRENT_CONTAINERS" ]; then
    log_info "Mevcut servisler bulundu, health check yapılıyor..."

    # API health check
    if docker exec vibexlearn-api-prod curl -sf http://localhost:8080/health > /dev/null 2>&1; then
        log_success "Mevcut API sağlıklı"
    else
        log_warn "Mevcut API sağlıksız, tam restart yapılacak"
    fi
fi

# Backup al
log_info "Veritabanı backup alınıyor..."
docker-compose -f "$COMPOSE_FILE" run --rm backup || log_warn "Backup alınamadı (ilk deployment olabilir)"

# Yeni image'ları çek
log_info "Yeni image'lar çekiliyor..."
docker-compose -f "$COMPOSE_FILE" pull --ignore-pull-failures

# Rolling deployment
log_info "Deployment başlatılıyor..."

# Önce database ve cache servislerini güncelle (downtime yok)
docker-compose -f "$COMPOSE_FILE" up -d --no-deps --build postgres redis elasticsearch kibana

# API'yi rolling update ile güncelle
log_info "API rolling update yapılıyor..."

# Yeni API container'ını başlat
export API_VERSION=$VERSION
docker-compose -f "$COMPOSE_FILE" up -d --no-deps --build --scale api=2 api

# Yeni container'ın healthy olmasını bekle
log_info "Yeni API container'ın healthy olması bekleniyor..."
sleep 10

for i in {1..30}; do
    if docker-compose -f "$COMPOSE_FILE" exec -T api curl -sf http://localhost:8080/health > /dev/null 2>&1; then
        log_success "API healthy!"
        break
    fi
    if [ $i -eq 30 ]; then
        log_error "API health check timeout!"
        docker-compose -f "$COMPOSE_FILE" logs api
        exit 1
    fi
    sleep 5
done

# Nginx'i güncelle
docker-compose -f "$COMPOSE_FILE" up -d --no-deps nginx

# Monitoring (eğer --profile monitoring kullanılıyorsa)
if [ "$ENABLE_MONITORING" = "true" ]; then
    log_info "Monitoring servisleri güncelleniyor..."
    docker-compose -f "$COMPOSE_FILE" --profile monitoring up -d prometheus grafana
fi

# Temizlik
log_info "Eski image'lar temizleniyor..."
docker image prune -f

# Final health check
log_info "Final health check..."
sleep 5

SERVICES=("postgres" "redis" "elasticsearch" "api" "nginx")
ALL_HEALTHY=true

for service in "${SERVICES[@]}"; do
    if docker-compose -f "$COMPOSE_FILE" ps | grep -q "vibexlearn-${service}-prod.*Up"; then
        log_success "$service: UP"
    else
        log_error "$service: DOWN"
        ALL_HEALTHY=false
    fi
done

if [ "$ALL_HEALTHY" = true ]; then
    log_success "============================================"
    log_success "Deployment başarıyla tamamlandı!"
    log_success "Version: $VERSION"
    log_success "============================================"
else
    log_error "Bazı servisler başarısız oldu!"
    docker-compose -f "$COMPOSE_FILE" logs
    exit 1
fi

# Deployment bilgilerini kaydet
echo "DEPLOYED_VERSION=$VERSION" > "$DEPLOY_DIR/.deploy_info"
echo "DEPLOYED_AT=$(date -u +"%Y-%m-%dT%H:%M:%SZ")" >> "$DEPLOY_DIR/.deploy_info"
