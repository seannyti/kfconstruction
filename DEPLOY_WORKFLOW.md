# KF Construction: Commit, Push, Pull, Publish, and Restart

This guide shows the minimal, reliable workflow to deploy changes to the Linode production server.

Production server: 23.239.26.52 (Rocky Linux 10)
Source directory on server: /var/www/myapp-src
Publish directories:
- API: /var/www/kfpublish/api
- Web: /var/www/kfpublish/web

## 1) Commit and push from your machine

Run from the repo root on your dev machine.

```powershell
# See what changed
git status

# Stage and commit
git add .
git commit -m "<your message>"

# Push to GitHub main
git push origin main
```

## 2) Pull latest on the server

SSH in and update the server working copy. We only pull, never commit on server.

```powershell
ssh root@23.239.26.52 "bash -lc 'cd /var/www/myapp-src && git fetch origin && git checkout main && git pull --ff-only origin main'"
```

If fast-forward fails due to divergence, the quickest fix is to replace the tree with a fresh clone (use with caution):

```powershell
ssh root@23.239.26.52 "bash -lc 'set -e; TS=$(date +%Y%m%d_%H%M%S); if [ -d /var/www/myapp-src ]; then mv /var/www/myapp-src /var/www/myapp-src.backup-$TS; fi; git clone https://github.com/seannyti/kfconstruction.git /var/www/myapp-src'"
```

## 3) Publish the applications

Build and publish both projects to the publish directories used by systemd services:

```powershell
# API
ssh root@23.239.26.52 "bash -lc 'cd /var/www/myapp-src/KfConstructionAPI && dotnet publish -c Release -o /var/www/kfpublish/api'"

# Web
ssh root@23.239.26.52 "bash -lc 'cd /var/www/myapp-src/KfConstructionWeb && dotnet publish -c Release -o /var/www/kfpublish/web'"
```

## 4) Restart services

```powershell
ssh root@23.239.26.52 "bash -lc 'systemctl restart kfapi.service kfweb.service nginx'"
```

## 5) Health checks

```powershell
# Web should return 200 and HTML content
ssh root@23.239.26.52 "bash -lc 'curl -s -o /dev/null -w "%{http_code}\n" http://127.0.0.1:5000/'"

# API health endpoint should say Healthy (or return 200)
ssh root@23.239.26.52 "bash -lc 'curl -s http://127.0.0.1:5001/health || true'"
```

## 6) Clean up old backups (optional)

If you used the fresh clone method, a backup folder like `/var/www/myapp-src.backup-YYYYMMDD_HHMMSS` may exist. Remove it when you’re confident the deploy is good:

```powershell
ssh root@23.239.26.52 "bash -lc 'rm -rf /var/www/myapp-src.backup-*'"
```

## Tips
- Do not commit directly on the server; always commit/push locally and only pull on server.
- If a publish fails, check logs:
  - ssh root@23.239.26.52 "journalctl -u kfweb.service -n 100 --no-pager"
  - ssh root@23.239.26.52 "journalctl -u kfapi.service -n 100 --no-pager"
- Keep using `git pull --ff-only` to avoid surprise merges on the server.
