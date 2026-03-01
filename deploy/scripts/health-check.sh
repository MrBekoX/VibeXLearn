#!/bin/bash
# =============================================================================
# VibeXLearn Platform - Health Check Script
# =============================================================================
# Kullanım: ./health-check.sh
# Monitoring için cron: */5 * * * * /path/to/health-check.sh
# =============================================================================

set -e

# Renkler
RED='\033[0;31m'
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
NC='\033[0m'

# Konfigürasyon
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
DEPLOY_DIR="$(dirname "$SCRIPT_DIR")"
COMPOSE_FILE="$DEPLOY_DIR/docker-compose.prod.yml"
ALERT_WEBHOOK=${ALERT_WEBHOOK:-}  # Slack/Discord webhook URL

# Environment yükle
if [ -f "$DEPLOY_DIR/.env.production" ]; then
    set -a
    source "$DEPLOY_DIR/.env.production"
    set +a
fi

# Slack/Discord bildirim
send_alert() {
    local message="$1"
    local color="$2"

    if [ -n "$ALERT_WEBHOOK" ]; then
        curl -s -X POST "$ALERT_WEBHOOK" \
            -H "Content-Type: application/json" \
            -d "{\"text\": \"🚨 VibeXLearn Health Alert: $message\"}" > /dev/null
    fi

    if [ "$color" = "red" ]; then
        echo -e "${RED}[CRITICAL]${NC} $message"
    elif [ "$color" = "yellow" ]; then
        echo -e "${YELLOW}[WARNING]${NC} $message"
    else
        echo -e "${GREEN}[OK]${NC} $message"
    fi
}

# Container durumunu kontrol et
check_container() {
    local container=$1
    local expected_name=$2

    if docker ps --format "{{.Names}}" | grep -q "$expected_name"; then
        return 0
    else
        return 1
    fi
}

# HTTP health check
check_http() {
    local url=$1
    local expected_status=${2:-200}

    response=$(curl -sf -o /dev/null -w "%{http_code}" "$url" 2>/dev/null || echo "000")

    if [ "$response" = "$expected_status" ]; then
        return 0
    else
        return 1
    fi
}

# Database connection check
check_postgres() {
    docker exec vibexlearn-postgres-prod pg_isready -U "$POSTGRES_USER" -d "$POSTGRES_DB" > /dev/null 2>&1
    return $?
}

check_redis() {
    docker exec vibexlearn-redis-prod redis-cli -a "$REDIS_PASSWORD" ping 2>/dev/null | grep -q "PONG"
    return $?
}

check_elasticsearch() {
    curl -sf -u "elastic:$ELASTIC_PASSWORD" "http://localhost:9200/_cluster/health" > /dev/null 2>&1
    return $?
}

# Ana health check
main() {
    echo "============================================"
    echo "VibeXLearn Health Check - $(date)"
    echo "============================================"

    ERRORS=0

    # ═══════════════════════════════════════════════════════════════════════════
    # Container Checks
    # ═══════════════════════════════════════════════════════════════════════════
    echo ""
    echo "📦 Container Status:"

    for container in "vibexlearn-postgres-prod" "vibexlearn-redis-prod" "vibexlearn-elasticsearch-prod" "vibexlearn-kibana-prod" "vibexlearn-api-prod" "vibexlearn-nginx-prod"; do
        if check_container "$container" "$container"; then
            echo -e "  ${GREEN}✓${NC} $container"
        else
            send_alert "Container DOWN: $container" "red"
            ERRORS=$((ERRORS + 1))
        fi
    done

    # ═══════════════════════════════════════════════════════════════════════════
    # Service Health Checks
    # ═══════════════════════════════════════════════════════════════════════════
    echo ""
    echo "🏥 Service Health:"

    # PostgreSQL
    if check_postgres; then
        echo -e "  ${GREEN}✓${NC} PostgreSQL"
    else
        send_alert "PostgreSQL bağlantı hatası!" "red"
        ERRORS=$((ERRORS + 1))
    fi

    # Redis
    if check_redis; then
        echo -e "  ${GREEN}✓${NC} Redis"
    else
        send_alert "Redis bağlantı hatası!" "red"
        ERRORS=$((ERRORS + 1))
    fi

    # Elasticsearch
    if check_elasticsearch; then
        echo -e "  ${GREEN}✓${NC} Elasticsearch"
    else
        send_alert "Elasticsearch bağlantı hatası!" "red"
        ERRORS=$((ERRORS + 1))
    fi

    # API Health
    if check_http "http://localhost:8080/health"; then
        echo -e "  ${GREEN}✓${NC} API Health"
    else
        send_alert "API health check başarısız!" "red"
        ERRORS=$((ERRORS + 1))
    fi

    # ═══════════════════════════════════════════════════════════════════════════
    # Resource Checks
    # ═══════════════════════════════════════════════════════════════════════════
    echo ""
    echo "📊 Resources:"

    # Disk usage
    DISK_USAGE=$(df -h /var/lib/docker | tail -1 | awk '{print $5}' | sed 's/%//')
    if [ "$DISK_USAGE" -gt 90 ]; then
        send_alert "Disk kullanımı kritik: ${DISK_USAGE}%" "red"
        ERRORS=$((ERRORS + 1))
    elif [ "$DISK_USAGE" -gt 80 ]; then
        send_alert "Disk kullanımı yüksek: ${DISK_USAGE}%" "yellow"
        echo -e "  ${YELLOW}!${NC} Disk: ${DISK_USAGE}%"
    else
        echo -e "  ${GREEN}✓${NC} Disk: ${DISK_USAGE}%"
    fi

    # Memory
    MEM_USAGE=$(free | grep Mem | awk '{printf "%.0f", $3/$2 * 100}')
    if [ "$MEM_USAGE" -gt 90 ]; then
        send_alert "Memory kullanımı kritik: ${MEM_USAGE}%" "red"
        ERRORS=$((ERRORS + 1))
    elif [ "$MEM_USAGE" -gt 80 ]; then
        send_alert "Memory kullanımı yüksek: ${MEM_USAGE}%" "yellow"
        echo -e "  ${YELLOW}!${NC} Memory: ${MEM_USAGE}%"
    else
        echo -e "  ${GREEN}✓${NC} Memory: ${MEM_USAGE}%"
    fi

    # ═══════════════════════════════════════════════════════════════════════════
    # Summary
    # ═══════════════════════════════════════════════════════════════════════════
    echo ""
    echo "============================================"

    if [ "$ERRORS" -eq 0 ]; then
        echo -e "${GREEN}✓ Tüm servisler sağlıklı${NC}"
        exit 0
    else
        echo -e "${RED}✗ $ERRORS hata tespit edildi${NC}"
        exit 1
    fi
}

main "$@"
