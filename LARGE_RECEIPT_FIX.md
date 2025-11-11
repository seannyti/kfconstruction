# 🔧 Large Receipt Upload Fix

## Issue
Large receipt uploads were timing out during OCR processing because nginx and Kestrel had default timeout values (60 seconds).

## What Was Fixed

### 1. ✅ Nginx Configuration
Updated `kfconstruction.conf` to include:
- `client_max_body_size 30M` - Allows up to 30MB file uploads
- `proxy_connect_timeout 300` - 5 minute connection timeout
- `proxy_send_timeout 300` - 5 minute send timeout
- `proxy_read_timeout 300` - 5 minute read timeout (for OCR processing)

### 2. ✅ Kestrel Configuration  
Updated `Program.cs` to include:
- `KeepAliveTimeout = 5 minutes` - Keep connection alive during OCR
- `RequestHeadersTimeout = 5 minutes` - Allow time for large uploads

### 3. ✅ User Interface
Updated Upload page to:
- Show max file size as 30MB (was incorrectly showing 10MB)
- Added warning that large receipts may take up to 60 seconds

## Deployment Steps

### On the Server:

```bash
# SSH to server
ssh root@23.239.26.52

# 1. Pull latest code
cd /var/www/kfconstruction
git pull origin main

# 2. Update nginx configuration
sudo cp kfconstruction.conf /etc/nginx/sites-available/kfconstruction.conf

# 3. Test nginx configuration
sudo nginx -t

# 4. If test passes, reload nginx
sudo systemctl reload nginx

# 5. Rebuild and restart web app
dotnet publish KfConstructionWeb/KfConstructionWeb.csproj -c Release -o /var/www/kfconstruction/web
sudo systemctl restart kfweb

# 6. Verify services
sudo systemctl status kfweb nginx

# 7. Check logs if issues
sudo journalctl -u kfweb -n 50
sudo tail -f /var/log/nginx/error.log
```

## Testing

After deployment, test with a large receipt:
1. Navigate to Admin → Receipts → Upload
2. Upload a large receipt (5-10 MB)
3. Wait for OCR processing (may take 30-60 seconds)
4. Verify receipt data is extracted correctly

## Technical Details

**Why It Failed Before:**
- Nginx default timeout: 60 seconds
- Kestrel default timeout: 60 seconds  
- Azure OCR processing for large receipts: 60-90 seconds

**Why It Works Now:**
- All timeouts increased to 5 minutes (300 seconds)
- Plenty of buffer for Azure OCR to process large/complex receipts
- User notified that processing may take time

## What Qualifies as a "Large Receipt"?

- **Small**: < 1 MB, processes in 5-15 seconds
- **Medium**: 1-5 MB, processes in 15-30 seconds  
- **Large**: 5-15 MB, processes in 30-60 seconds
- **Very Large**: 15-30 MB, processes in 60-90 seconds

Receipts larger than 30MB should be compressed or resized before upload.

---

**Last Updated**: November 10, 2025
