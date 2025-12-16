#!/bin/bash
set -euo pipefail

# Цвета для вывода
GREEN='\033[0;32m'
YELLOW='\033[1;33m'
RED='\033[0;31m'
NC='\033[0m'

log() {
    echo -e "${GREEN}[$(date +'%Y-%m-%d %H:%M:%S')]${NC} $1"
}

error() {
    echo -e "${RED}[ERROR]${NC} $1"
}

if [ ! -f "manual-fix-telegram-fields.sql" ]; then
    error "SQL file not found!"
    exit 1
fi

log "Applying manual SQL fix for Telegram fields..."

# Получаем переменные окружения из .env
if [ -f ".env" ]; then
    export $(grep -v '^#' .env | xargs)
fi

# Применяем SQL скрипт
docker exec -i medicationassist-postgres psql -U "${POSTGRES_USER:-postgres}" -d "${POSTGRES_DB:-medicationassist}" < manual-fix-telegram-fields.sql

if [ $? -eq 0 ]; then
    log "SQL fix applied successfully!"
    log "Restarting API service..."
    docker-compose restart api
    log "Done!"
else
    error "Failed to apply SQL fix"
    exit 1
fi
