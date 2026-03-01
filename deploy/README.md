# VibeXLearn Platform - Deployment

Bu klasör, VibeXLearn platformunun hem development hem de production ortamları için gerekli tüm Docker altyapısını içerir.

## 📁 Klasör Yapısı

```
deploy/
├── docker-compose.yml           # Development compose dosyası
├── docker-compose.prod.yml      # Production compose dosyası
├── .env                         # Development environment
├── .env.production.example      # Production environment şablonu
├── configs/
│   ├── postgres/               # PostgreSQL init script'leri
│   ├── redis/                  # Redis konfigürasyonu
│   ├── elasticsearch/          # Elasticsearch konfigürasyonu
│   └── kibana/                 # Kibana konfigürasyonu
├── nginx/
│   ├── nginx.conf              # Production reverse proxy
│   ├── ssl/                    # SSL sertifikaları
│   └── logs/                   # Nginx log'ları
├── monitoring/
│   ├── prometheus/
│   │   └── prometheus.yml      # Prometheus konfigürasyonu
│   └── grafana/
│       └── provisioning/       # Datasource & dashboard
└── scripts/
    ├── deploy.sh               # Production deployment
    ├── rollback.sh             # Versiyon geri alma
    ├── backup.sh               # Veritabanı backup
    ├── restore.sh              # Backup'tan restore
    └── health-check.sh         # Sistem sağlık kontrolü
```

---

## 🚀 Development

### Hızlı Başlangıç

```bash
cd deploy

# Sadece altyapı servislerini başlat (Postgres, Redis, ES, Kibana)
docker-compose up -d

# API ile birlikte başlat
docker-compose --profile api up -d --build
```

### Servisler

| Servis | Port | Açıklama |
|--------|------|----------|
| PostgreSQL | 5432 | Veritabanı |
| Redis | 6379 | Cache |
| Elasticsearch | 9200 | Search & Logging |
| Kibana | 5601 | ES Dashboard |
| API | 8080 | .NET API (opsiyonel) |

### Development Komutları

```bash
# Servisleri başlat
docker-compose up -d

# Log'ları izle
docker-compose logs -f

# Servis durdur
docker-compose down

# Volume'ları da sil
docker-compose down -v
```

---

## 🏭 Production

### 1. Environment Hazırlığı

```bash
cd deploy

# .env.production dosyasını oluştur
cp .env.production.example .env.production

# Tüm değerleri doldur
nano .env.production

# Şifre oluşturmak için:
openssl rand -base64 32
```

### 2. SSL Sertifikaları

```bash
# Let's Encrypt ile
certbot certonly --standalone -d api.vibexlearn.com
cp /etc/letsencrypt/live/api.vibexlearn.com/fullchain.pem nginx/ssl/
cp /etc/letsencrypt/live/api.vibexlearn.com/privkey.pem nginx/ssl/

# Veya kendi sertifikanızı kopyalayın
```

### 3. Deployment

```bash
# Deploy başlat
./scripts/deploy.sh 1.0.0

# Veya manuel:
docker-compose -f docker-compose.prod.yml up -d
```

### 4. Monitoring (Opsiyonel)

```bash
# Monitoring servisleri ile
docker-compose -f docker-compose.prod.yml --profile monitoring up -d
```

---

## 📋 Komutlar

| Komut | Açıklama |
|-------|----------|
| `./scripts/deploy.sh [version]` | Production deploy |
| `./scripts/rollback.sh [version]` | Versiyon geri alma |
| `./scripts/backup.sh` | Backup al |
| `./scripts/restore.sh [file]` | Backup'tan restore |
| `./scripts/health-check.sh` | Sağlık kontrolü |

---

## 🔄 Rolling Update

```bash
# Yeni version deploy et
./scripts/deploy.sh 1.0.5

# Sorun çıkarsa geri dön
./scripts/rollback.sh 1.0.4
```

---

## 💾 Backup & Restore

### Backup

```bash
# Manuel backup
./scripts/backup.sh

# Cron ile otomatik (her gün 02:00)
# crontab -e
0 2 * * * /path/to/deploy/scripts/backup.sh >> /var/log/vibexlearn-backup.log 2>&1
```

### Restore

```bash
# Backup'ları listele
ls -la /var/backups/vibexlearn/

# Restore yap
./scripts/restore.sh postgres_20240101_020000.dump
```

---

## 📊 Monitoring

| Servis | URL | Açıklama |
|--------|-----|----------|
| Prometheus | internal:9090 | Metrics collection |
| Grafana | grafana.vibexlearn.internal | Dashboard |
| Kibana | kibana.vibexlearn.internal | Log viewer |

---

## 🔒 Güvenlik

### Rate Limiting (Nginx)

- Genel API: 10 req/s
- Auth endpoint'leri: 5 req/dakika
- Max connection: 20/IP

### SSL/TLS

- TLS 1.2+ destekleniyor
- Modern cipher suite'ler
- HSTS enabled

---

## 🐛 Troubleshooting

```bash
# Log'ları izle
docker-compose -f docker-compose.prod.yml logs -f api

# Servis yeniden başlat
docker-compose -f docker-compose.prod.yml restart api

# DB bağlantı testi
docker exec -it vibexlearn-postgres-prod psql -U $POSTGRES_USER -d $POSTGRES_DB

# Redis testi
docker exec -it vibexlearn-redis-prod redis-cli -a $REDIS_PASSWORD ping
```

---

## 📈 Resource Limits

| Servis | CPU | Memory |
|--------|-----|--------|
| PostgreSQL | 2 cores | 2 GB |
| Redis | 1 core | 768 MB |
| Elasticsearch | 2 cores | 2 GB |
| API (x2) | 2 cores | 1 GB |
| Nginx | 1 core | 256 MB |
