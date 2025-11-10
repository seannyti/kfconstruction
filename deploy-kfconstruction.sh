#!/bin/bash
set -euo pipefail

echo "=== KF Construction Deployment Script ==="
echo "Starting deployment at $(date)"

# Variables
# Source repo directory (contains KfConstructionAPI/ and KfConstructionWeb/)
SOURCE_DIR="/var/www/myapp-src"
# Publish output directories (used by systemd services)
API_PUBLISH_DIR="/var/www/kfpublish/api"
WEB_PUBLISH_DIR="/var/www/kfpublish/web"
BACKUP_DIR="/var/backups/kfconstruction"

# Ensure required directories exist
mkdir -p "$BACKUP_DIR" "$API_PUBLISH_DIR" "$WEB_PUBLISH_DIR"

# Sanity checks
if [ ! -d "$SOURCE_DIR" ]; then
    echo "ERROR: Source directory not found: $SOURCE_DIR"
    exit 2
fi
if ! command -v dotnet >/dev/null 2>&1; then
    echo "ERROR: dotnet SDK is not installed or not in PATH"
    exit 3
fi
if ! command -v git >/dev/null 2>&1; then
    echo "ERROR: git is not installed"
    exit 4
fi

# Backup current deployment
echo "Creating backup..."
BACKUP_TIMESTAMP=$(date +%Y%m%d_%H%M%S)
if [ -d "$API_PUBLISH_DIR" ]; then
    tar -czf $BACKUP_DIR/api_$BACKUP_TIMESTAMP.tar.gz -C $API_PUBLISH_DIR .
fi
if [ -d "$WEB_PUBLISH_DIR" ]; then
    tar -czf $BACKUP_DIR/web_$BACKUP_TIMESTAMP.tar.gz -C $WEB_PUBLISH_DIR .
fi

# Pull latest code
echo "Pulling latest code from GitHub..."
cd "$SOURCE_DIR"
git fetch origin
git checkout main
git pull --ff-only origin main

# Build and publish API
echo "Publishing API..."
cd "$SOURCE_DIR/KfConstructionAPI"
dotnet publish -c Release -o "$API_PUBLISH_DIR"

# Build and publish Web
echo "Publishing Web..."
cd "$SOURCE_DIR/KfConstructionWeb"
dotnet publish -c Release -o "$WEB_PUBLISH_DIR"

# Restart services
echo "Restarting services..."
systemctl restart kfapi.service
systemctl restart kfweb.service
systemctl reload nginx || true

# Wait for services to start
sleep 3

# Check service status
echo "Checking service status..."
if systemctl is-active --quiet kfapi.service; then
    echo "✓ API service is running"
else
    echo "✗ API service failed to start"
    echo "Rolling back..."
    if [ -f "$BACKUP_DIR/api_${BACKUP_TIMESTAMP}.tar.gz" ]; then
        tar -xzf "$BACKUP_DIR/api_${BACKUP_TIMESTAMP}.tar.gz" -C "$API_PUBLISH_DIR"
    else
        echo "No API backup archive found for timestamp $BACKUP_TIMESTAMP; skipping rollback extract."
    fi
    systemctl restart kfapi.service
    exit 1
fi

if systemctl is-active --quiet kfweb.service; then
    echo "✓ Web service is running"
else
    echo "✗ Web service failed to start"
    echo "Rolling back..."
    if [ -f "$BACKUP_DIR/web_${BACKUP_TIMESTAMP}.tar.gz" ]; then
        tar -xzf "$BACKUP_DIR/web_${BACKUP_TIMESTAMP}.tar.gz" -C "$WEB_PUBLISH_DIR"
    else
        echo "No Web backup archive found for timestamp $BACKUP_TIMESTAMP; skipping rollback extract."
    fi
    systemctl restart kfweb.service
    exit 1
fi

# Clean up old backups (keep last 5)
echo "Cleaning old backups..."
cd "$BACKUP_DIR"
ls -t api_*.tar.gz 2>/dev/null | tail -n +6 | xargs -r rm -- || true
ls -t web_*.tar.gz 2>/dev/null | tail -n +6 | xargs -r rm -- || true

echo "Deployment completed successfully at $(date)"
