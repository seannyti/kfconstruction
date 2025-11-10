# Linode Production Server Configuration

**Last Updated:** November 7, 2025  
**Server IP:** 23.239.26.52  
**SSH Access:** root@23.239.26.52 (passwordless SSH key authentication configured)

## Server Specifications

- **OS:** Rocky Linux 10.0 (Red Quartz)
- **Platform:** RHEL-based (EL10)
- **Memory:** 3.6 GB RAM
- **Swap:** 512 MB
- **Support End:** May 31, 2035

## Installed Software

- **.NET SDK:** 9.0.111
- **Nginx:** Running as reverse proxy
- **SELinux:** Configured with `httpd_can_network_connect` enabled

## Directory Structure

```
/var/www/
├── myapp-src/           # Source code repository
└── kfpublish/
    ├── api/             # Published KfConstructionAPI (port 5001)
    └── web/             # Published KfConstructionWeb (port 5000)
```

## Systemd Services

### KfConstruction Web Service
**File:** `/etc/systemd/system/kfweb.service`
```ini
[Unit]
Description=KfConstruction Web
After=network.target

[Service]
WorkingDirectory=/var/www/kfpublish/web
ExecStart=/usr/bin/dotnet /var/www/kfpublish/web/KfConstructionWeb.dll
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5000

[Install]
WantedBy=multi-user.target
```

**Status:** Running  
**Port:** 5000 (internal)  
**Config:** `/var/www/kfpublish/web/appsettings.Production.json`

### KfConstruction API Service
**File:** `/etc/systemd/system/kfapi.service`
```ini
[Unit]
Description=KfConstruction API
After=network.target

[Service]
WorkingDirectory=/var/www/kfpublish/api
ExecStart=/usr/bin/dotnet /var/www/kfpublish/api/KfConstructionAPI.dll
Restart=always
RestartSec=5
Environment=ASPNETCORE_ENVIRONMENT=Production
Environment=ASPNETCORE_URLS=http://127.0.0.1:5001

[Install]
WantedBy=multi-user.target
```

**Status:** Running  
**Port:** 5001 (internal)  
**Config:** `/var/www/kfpublish/api/appsettings.Production.json`

## Nginx Configuration

### Main Config
**File:** `/etc/nginx/nginx.conf`
- Worker processes: auto
- Worker connections: 1024
- User: nginx
- Error log: `/var/log/nginx/error.log`
- Access log: `/var/log/nginx/access.log`

### Reverse Proxy Config
**File:** `/etc/nginx/conf.d/myapp.conf`
```nginx
server {
    listen 80;
    server_name _;

    location / {
        proxy_pass         http://127.0.0.1:5000;
        proxy_http_version 1.1;
        proxy_set_header   Upgrade $http_upgrade;
        proxy_set_header   Connection keep-alive;
        proxy_set_header   Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header   X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header   X-Forwarded-Proto $scheme;
    }
}
```

**Public Access:** http://23.239.26.52 → proxied to Web app on port 5000

## Database Configuration

### Azure SQL Database
- **Server:** kfconstruction.database.windows.net
- **Database:** kfconstructiondb
- **Tier:** Development (cost-effective ~$5-15/month)
- **Authentication:** SQL Authentication
- **User:** kfconstruction
- **Password:** *(Stored securely in production appsettings.Production.json)*

### Firewall Rules
- Client IP: Your local machine IP
- **Linode Server IP:** 23.239.26.52 (AllowLinodeServer rule)

### Connection String
Stored in `/var/www/kfpublish/web/appsettings.Production.json` and `/var/www/kfpublish/api/appsettings.Production.json`:
```
Server=tcp:kfconstruction.database.windows.net,1433;Initial Catalog=kfconstructiondb;Persist Security Info=False;User ID=kfconstruction;Password=***;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
```

## Application Configuration

### Super Admin Account
- **Email:** seannytheirish@gmail.com
- **Password:** SAdmin@123
- **Role:** SuperAdmin

### Service Management Commands

```bash
# Check service status
systemctl status kfweb.service
systemctl status kfapi.service
systemctl status nginx.service

# Restart services
systemctl restart kfweb.service
systemctl restart kfapi.service
systemctl restart nginx.service

# View logs
journalctl -u kfweb.service -n 50 --no-pager
journalctl -u kfapi.service -n 50 --no-pager
journalctl -u nginx.service -n 50 --no-pager

# Follow logs in real-time
journalctl -u kfweb.service -f
```

## Deployment Workflow

### Automated Deployment (Recommended)

**Deployment Script:** `/usr/local/bin/deploy-kfweb.sh`

This script handles the complete deployment process with automatic backup and rollback on failure.

**Steps:**
1. **Make changes locally** in VS Code
2. **Test locally** using `dotnet run` (uses Development settings)
3. **Commit and push to GitHub:**
   ```bash
   git add .
   git commit -m "Description of changes"
   git push origin main
   ```
4. **Deploy to production:**
   ```bash
   ssh root@23.239.26.52 "/usr/local/bin/deploy-kfweb.sh"
   ```

**What the script does:**
- Pulls latest code from GitHub repository
- Creates timestamped backup of current deployment
- Builds and publishes the application
- Restarts the kfweb service
- Verifies service is running
- **Automatically rolls back** if deployment fails

**Script Location on Server:** `/usr/local/bin/deploy-kfweb.sh`

### Manual Deployment (Legacy)

If you need to deploy manually:

1. **SSH into server:**
   ```bash
   ssh root@23.239.26.52
   ```

2. **Pull latest code:**
   ```bash
   cd /var/www/myapp-src
   git pull origin main
   ```

3. **Publish applications:**
   ```bash
   dotnet publish ./KfConstructionWeb/KfConstructionWeb.csproj -c Release -o /var/www/kfpublish/web
   dotnet publish ./KfConstructionAPI/KfConstructionAPI.csproj -c Release -o /var/www/kfpublish/api
   ```

4. **Restart services:**
   ```bash
   systemctl restart kfweb.service kfapi.service
   ```

## Network & Firewall

### Open Ports
- **80:** HTTP (Nginx)
- **5000:** Kestrel Web (internal only - 127.0.0.1)
- **5001:** Kestrel API (internal only - 127.0.0.1)

### SELinux Configuration
```bash
# Allow httpd to make network connections (already configured)
setsebool -P httpd_can_network_connect 1
```

## Troubleshooting

### Connection String Issues
If the app cannot connect to Azure SQL:
1. Check Azure SQL firewall includes server IP (23.239.26.52)
2. Verify `appsettings.Production.json` exists in publish directories
3. Run `/tmp/fix-production-config.sh` to recreate config files

### Service Not Starting
```bash
# Check service status
systemctl status kfweb.service -l

# View full logs
journalctl -u kfweb.service -n 100 --no-pager

# Restart after config changes
systemctl daemon-reload
systemctl restart kfweb.service
```

### Database Migration
```bash
# Apply migrations from source directory
cd /var/www/myapp-src/KfConstructionWeb
dotnet ef database update

cd /var/www/myapp-src/KfConstructionAPI
dotnet ef database update
```

## Backup & Recovery

### Git Repository
- **Remote:** https://github.com/seannyti/kfconstruction
- **Branch:** main
- All production configurations stored in repository

### Update from Git
```bash
cd /var/www/myapp-src
git pull origin main
```

## Security Notes

- SSH key authentication configured (no password required)
- Azure SQL uses encrypted connection (Encrypt=True)
- SELinux enabled and configured
- Services run in Production mode (detailed errors not exposed)
- Rate limiting configured for uploads (10 receipts/hour)
- AES-256-GCM encryption for sensitive data

## Monitoring

### Health Checks
- Web app: http://23.239.26.52/health
- Check database connectivity in logs
- Monitor memory usage: `free -h`
- Monitor disk usage: `df -h`

### Performance
- Current memory usage: ~660 MB used / 3.6 GB available
- Swap usage: Minimal (~512 KB / 512 MB)
