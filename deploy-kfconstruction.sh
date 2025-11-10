#!/bin/bash
set -e

echo "=== KF Construction Deployment Script ==="
echo "Starting deployment at $(date)"

# Variables
SOURCE_DIR="/var/www/myapp-src"
API_PUBLISH_DIR="/var/www/kfpublish/api"
WEB_PUBLISH_DIR="/var/www/kfpublish/web"
BACKUP_DIR="/var/backups/kfconstruction"

# Create backup directory if it doesn't exist
mkdir -p $BACKUP_DIR

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
cd $SOURCE_DIR
git pull origin main

# Build and publish API
echo "Publishing API..."
cd $SOURCE_DIR/KfConstructionAPI
dotnet publish -c Release -o $API_PUBLISH_DIR

# Build and publish Web
echo "Publishing Web..."
cd $SOURCE_DIR/KfConstructionWeb
dotnet publish -c Release -o $WEB_PUBLISH_DIR

# Restart services
echo "Restarting services..."
systemctl restart kfapi.service
systemctl restart kfweb.service
systemctl reload nginx

# Wait for services to start
sleep 3

# Check service status
echo "Checking service status..."
if systemctl is-active --quiet kfapi.service; then
    echo "✓ API service is running"
else
    echo "✗ API service failed to start"
    echo "Rolling back..."
    tar -xzf $BACKUP_DIR/api_$BACKUP_TIMESTAMP.tar.gz -C $API_PUBLISH_DIR
    systemctl restart kfapi.service
    exit 1
fi

if systemctl is-active --quiet kfweb.service; then
    echo "✓ Web service is running"
else
    echo "✗ Web service failed to start"
    echo "Rolling back..."
    tar -xzf $BACKUP_DIR/web_$BACKUP_TIMESTAMP.tar.gz -C $WEB_PUBLISH_DIR
    systemctl restart kfweb.service
    exit 1
fi

# Clean up old backups (keep last 5)
echo "Cleaning old backups..."
cd $BACKUP_DIR
ls -t api_*.tar.gz 2>/dev/null | tail -n +6 | xargs -r rm
ls -t web_*.tar.gz 2>/dev/null | tail -n +6 | xargs -r rm

echo "Deployment completed successfully at $(date)"
