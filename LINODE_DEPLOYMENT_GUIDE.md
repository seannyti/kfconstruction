# KF Construction - Linode Server Deployment Guide

## Server Information
- **IP Address**: 23.239.26.52
- **OS**: Rocky Linux 10.0 (Red Quartz)
- **SSH Access**: Passwordless authentication configured

## Initial Server Setup (Already Complete)

### 1. Software Installed
```bash
dnf install -y dotnet-sdk-9.0 git nginx
dotnet tool install --global dotnet-ef
```

### 2. Directory Structure
```
/var/www/myapp-src/          # Git repository source code
/var/www/kfpublish/api/      # Published API application
/var/www/kfpublish/web/      # Published Web application
/var/backups/kfconstruction/ # Deployment backups
```

### 3. Environment Files (Secured with chmod 600)
- `/etc/systemd/system/kfapi.env` - API environment variables
- `/etc/systemd/system/kfweb.env` - Web environment variables

**Variables configured:**
- Database connection string (Azure SQL)
- API key (shared between API and Web)
- Email SMTP settings (Yahoo Mail with app password)
- Receipt encryption key

### 4. Systemd Services
- `kfapi.service` - API backend service (port 5001)
- `kfweb.service` - Web frontend service (port 5000)
- Both services auto-start on boot
- Service files: `/etc/systemd/system/kfapi.service` and `kfweb.service`

### 5. Nginx Configuration
- Reverse proxy configured at `/etc/nginx/conf.d/kfconstruction.conf`
- Routes port 80 traffic to Web app (port 5000)
- SELinux configured: `httpd_can_network_connect` enabled

### 6. Database Migrations
All migrations applied to Azure SQL database:
- API: `AddApiKeys` migration
- Web: `AddFileManagementAndActivityLogs`, `AddCMS`, `AddBroadcastMessaging` migrations

## Deploying Updates

### Quick Deploy (Recommended)
```bash
ssh root@23.239.26.52
/usr/local/bin/deploy-kfconstruction.sh
```

This script automatically:
1. Backs up current deployment
2. Pulls latest code from GitHub
3. Publishes both API and Web apps
4. Restarts services
5. Validates services are running
6. Rolls back on failure
7. Cleans old backups (keeps last 5)

### Manual Deploy Steps
If you need to deploy manually:

```bash
# SSH into server
ssh root@23.239.26.52

# Pull latest code
cd /var/www/myapp-src
git pull origin main

# Publish API
cd /var/www/myapp-src/KfConstructionAPI
dotnet publish -c Release -o /var/www/kfpublish/api

# Publish Web
cd /var/www/myapp-src/KfConstructionWeb
dotnet publish -c Release -o /var/www/kfpublish/web

# Restart services
systemctl restart kfapi.service kfweb.service
systemctl reload nginx
```

### Running Database Migrations
If you add new migrations, run them from your local machine:

```bash
# For API migrations
cd KfConstructionAPI
$env:ASPNETCORE_ENVIRONMENT='Production'
dotnet ef database update

# For Web migrations
cd KfConstructionWeb
$env:ASPNETCORE_ENVIRONMENT='Production'
dotnet ef database update
```

## Service Management

### Check Service Status
```bash
ssh root@23.239.26.52 "systemctl status kfapi.service kfweb.service nginx"
```

### View Service Logs
```bash
# API logs
ssh root@23.239.26.52 "journalctl -u kfapi.service -n 50 --no-pager"

# Web logs
ssh root@23.239.26.52 "journalctl -u kfweb.service -n 50 --no-pager"

# Follow live logs
ssh root@23.239.26.52 "journalctl -u kfweb.service -f"
```

### Restart Services
```bash
ssh root@23.239.26.52 "systemctl restart kfapi.service kfweb.service"
```

### Stop/Start Services
```bash
ssh root@23.239.26.52 "systemctl stop kfapi.service kfweb.service"
ssh root@23.239.26.52 "systemctl start kfapi.service kfweb.service"
```

## Testing

### Test API Health
```bash
ssh root@23.239.26.52 "curl -s http://127.0.0.1:5001/health"
# Should return: Healthy
```

### Test Web App
```bash
ssh root@23.239.26.52 "curl -s -o /dev/null -w '%{http_code}' http://127.0.0.1:5000/"
# Should return: 200
```

### Test Public Access
Open browser to: http://23.239.26.52

## Troubleshooting

### 502 Bad Gateway
Check if services are running:
```bash
ssh root@23.239.26.52 "systemctl status kfapi.service kfweb.service"
```

Check service logs for errors:
```bash
ssh root@23.239.26.52 "journalctl -u kfweb.service -n 100 --no-pager"
```

### Database Connection Issues
Verify Azure SQL firewall allows Linode IP (23.239.26.52)

Check connection string in environment files:
```bash
ssh root@23.239.26.52 "cat /etc/systemd/system/kfapi.env"
```

### Service Won't Start
Check for compilation errors:
```bash
ssh root@23.239.26.52 "cd /var/www/myapp-src/KfConstructionWeb && dotnet build"
```

## Important Notes

1. **Never commit production environment files** - They contain secrets and are gitignored
2. **Environment variables are managed on the server** - Not in appsettings.Production.json
3. **Backups are automatic** - Deployment script creates backups before each deploy
4. **Services auto-restart** - Configured with `Restart=always` in systemd
5. **Migrations run from local machine** - Server doesn't need EF tools for runtime

## Security

- Environment files secured with chmod 600 (root only)
- SSH key authentication (no password login)
- SELinux enabled and configured
- All secrets stored in environment variables
- Production appsettings.json files are empty/example only

## Future Setup (If Starting Fresh)

If you ever need to rebuild the server from scratch, follow these steps:

1. Install software (see Initial Server Setup section 1)
2. Create directory structure (section 2)
3. Clone repository: `git clone https://github.com/seannyti/kfconstruction.git /var/www/myapp-src`
4. Create environment files with secrets (section 3)
5. Create systemd service files (section 4)
6. Configure Nginx (section 5)
7. Enable SELinux: `setsebool -P httpd_can_network_connect 1`
8. Run initial deployment: `/usr/local/bin/deploy-kfconstruction.sh`
9. Apply database migrations from local machine (section "Running Database Migrations")

## Contact & Support

Repository: https://github.com/seannyti/kfconstruction
