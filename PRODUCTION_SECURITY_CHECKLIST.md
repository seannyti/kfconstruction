# 🔒 Production Security Checklist

## ⚠️ BEFORE DEPLOYING TO PRODUCTION

### Critical Security Items

#### 1. **Remove Hardcoded Secrets from appsettings.Production.json**
- ✅ **FIXED**: Removed database password from appsettings.Production.json  
- ✅ **FIXED**: Removed API key from appsettings.Production.json
- ✅ **FIXED**: Removed email credentials from appsettings.Production.json
- **Action Required**: All secrets MUST be set via environment variables on production server

#### 2. **Environment Variables (Server-Side Only)**
The following must be set in `/etc/systemd/system/kfweb.env` and `/etc/systemd/system/kfapi.env`:

**kfweb.env:**
```bash
ConnectionStrings__DefaultConnection=Server=tcp:kfconstruction.database.windows.net,1433;Initial Catalog=kfconstructiondb;User ID=kfconstruction;Password=YOUR_SECURE_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
ApiSettings__BaseUrl=http://127.0.0.1:5001
ApiSettings__ApiKey=YOUR_API_KEY_HERE
EmailSettings__Smtp__Host=smtp.mail.yahoo.com
EmailSettings__Smtp__Port=587
EmailSettings__Smtp__Username=knudsonfamilyconstruction@yahoo.com
EmailSettings__Smtp__Password=YOUR_EMAIL_APP_PASSWORD
EmailSettings__Smtp__EnableSsl=true
EmailSettings__DefaultSender__Email=knudsonfamilyconstruction@yahoo.com
EmailSettings__DefaultSender__Name=Knudson Family Construction
EmailSettings__EnableEmails=true
ReceiptEncryption__EncryptionKey=YOUR_ENCRYPTION_KEY_BASE64
```

**kfapi.env:**
```bash
ConnectionStrings__DefaultConnection=Server=tcp:kfconstruction.database.windows.net,1433;Initial Catalog=kfconstructiondb;User ID=kfconstruction;Password=YOUR_SECURE_PASSWORD;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;
ApiSettings__ApiKey=YOUR_API_KEY_HERE
```

#### 3. **Change Default Passwords**
⚠️ **CRITICAL**: `SeedData.cs` has weak default passwords:
- SuperAdmin default: `SuperAdmin@123`
- Admin default: `Admin@123`

**Recommendation**: Set strong passwords via environment variables BEFORE first deployment:
```bash
export INITIAL_SUPERADMIN_PASSWORD="YourStr0ng!P@ssword123"
export INITIAL_ADMIN_PASSWORD="An0therStr0ng!P@ssword456"
```

Then **immediately change passwords** via admin UI after first login.

#### 4. **Git Safety Check**
✅ **VERIFIED**: `.env` files are in `.gitignore`
✅ **VERIFIED**: `appsettings.Production.json` files are in `.gitignore`
✅ **VERIFIED**: No secrets currently tracked in git

**Warning**: If secrets were previously committed, they exist in git history. Consider:
- Using `git filter-repo` to remove from history
- Rotating all exposed credentials
- Using GitHub secret scanning

---

## ✅ Security Features Already Implemented

### Authentication & Authorization
- ✅ All admin controllers require authentication
- ✅ Role-based authorization (Admin, SuperAdmin, User)
- ✅ SuperAdmin-only actions for sensitive operations (Backup, API Keys, User Management)
- ✅ Account lockout after 5 failed login attempts
- ✅ 15-minute lockout duration
- ✅ Strong password requirements (8 chars, uppercase, lowercase, digit, special char)

### Security Headers (OWASP ASVS L2)
- ✅ `X-Content-Type-Options: nosniff`
- ✅ `X-Frame-Options: SAMEORIGIN` (Web) / `DENY` (API)
- ✅ `X-XSS-Protection: 1; mode=block`
- ✅ `Referrer-Policy: strict-origin-when-cross-origin`
- ✅ `Content-Security-Policy` (Web only)
- ✅ `Permissions-Policy` (Web only)
- ✅ HSTS enabled (30 days)
- ✅ HTTPS redirection enforced

### API Security
- ✅ API Key authentication required
- ✅ Rate limiting (60 req/min, 500 req/hour)
- ✅ CORS configured with explicit allowed origins
- ✅ API versioning enabled
- ✅ Swagger disabled in production

### Data Protection
- ✅ Receipt file encryption (AES-256)
- ✅ Passwords hashed with Identity (PBKDF2)
- ✅ SQL injection protection (parameterized queries, EF Core)
- ✅ CSRF protection (anti-forgery tokens)
- ✅ Input validation on all forms
- ✅ File upload restrictions (30MB limit, type validation)

### Logging & Monitoring
- ✅ Activity logging for admin actions
- ✅ Audit trail for user locks/unlocks
- ✅ No sensitive data logged (passwords logged as boolean)
- ✅ Health checks configured
- ✅ Performance tracking middleware

### Database
- ✅ Migrations applied automatically on startup
- ✅ Connection strings encrypted in transit (Encrypt=True)
- ✅ Azure SQL with TLS 1.2
- ✅ Database backup/restore functionality (SuperAdmin only)

---

## 🚀 Pre-Deployment Steps

### 1. Update Production Environment Files
```bash
# SSH to server
ssh root@23.239.26.52

# Edit environment files
nano /etc/systemd/system/kfweb.env
nano /etc/systemd/system/kfapi.env

# Set strong passwords (DO NOT use the example values above)
# Generate strong passwords: openssl rand -base64 32
```

### 2. Update CORS Origins
Edit `kfapi.env` and `appsettings.Production.json`:
```json
"ApiSettings": {
  "AllowedOrigins": [
    "https://your-actual-domain.com",
    "https://www.your-actual-domain.com"
  ]
}
```

### 3. Build & Test
```bash
dotnet build -c Release
dotnet test
```

### 4. Deploy
```bash
# From local machine
cd c:\Users\seann\source\repos\KfConstructionAPI\KfConstruction
git add .
git commit -m "Production security hardening"
git push origin main

# On server
cd /var/www/kfconstruction
git pull
dotnet publish KfConstructionWeb/KfConstructionWeb.csproj -c Release -o /var/www/kfconstruction/web
dotnet publish KfConstructionAPI/KfConstructionAPI.csproj -c Release -o /var/www/kfconstruction/api
sudo systemctl restart kfweb kfapi
```

### 5. Post-Deployment Verification
```bash
# Check services are running
sudo systemctl status kfweb kfapi

# Check health endpoints
curl https://your-domain.com/health/live
curl http://localhost:5001/health

# Verify HTTPS redirect
curl -I http://your-domain.com

# Check security headers
curl -I https://your-domain.com

# Test login with default admin account
# IMMEDIATELY change password via UI

# Verify email sending works
```

---

## 🔐 Security Best Practices

### Password Rotation
- Rotate database password every 90 days
- Rotate API key every 180 days  
- Rotate email app password if compromised
- Rotate encryption key only if compromised (requires re-encrypting files)

### Monitoring
- Monitor `/var/log/journal` for errors
- Set up Azure SQL alerts for suspicious activity
- Monitor failed login attempts via Activity Logs

### Backup Strategy
- Database backups run manually via Admin UI
- Store backups off-server (Azure Blob Storage recommended)
- Test restore procedure quarterly
- Retention: 30 days minimum

### Updates
- Update .NET runtime monthly
- Update NuGet packages quarterly
- Review security advisories weekly

---

## 📋 Production Readiness Score

| Category | Status | Notes |
|----------|--------|-------|
| **Authentication** | ✅ Ready | Role-based, lockout enabled |
| **Authorization** | ✅ Ready | All endpoints protected |
| **Secrets Management** | ⚠️ **ACTION REQUIRED** | Must use env vars only |
| **HTTPS/TLS** | ✅ Ready | Enforced, HSTS enabled |
| **Security Headers** | ✅ Ready | OWASP ASVS L2 compliant |
| **Input Validation** | ✅ Ready | All forms validated |
| **CSRF Protection** | ✅ Ready | Anti-forgery tokens |
| **Rate Limiting** | ✅ Ready | API protected |
| **Logging** | ✅ Ready | No sensitive data logged |
| **Error Handling** | ✅ Ready | Generic errors in production |
| **Database Security** | ✅ Ready | Encrypted, parameterized queries |
| **File Upload Security** | ✅ Ready | Validated, encrypted, size limited |

---

## ⚠️ Known Issues / Technical Debt

### Low Priority
1. Default admin passwords in SeedData.cs (mitigated by env vars)
2. CORS AllowedOrigins contains placeholder domain (update before deploy)
3. Service Worker missing icon files (PWA incomplete but functional)

### Monitoring Needed
1. Backup database page - verify works in production
2. Receipt OCR service - requires Azure API key configuration
3. Email sending - verify Yahoo SMTP app password works

---

## 📝 Deployment Checklist

- [ ] All secrets removed from `appsettings.Production.json`
- [ ] Environment variables set on server (`/etc/systemd/system/*.env`)
- [ ] Strong passwords set for default admin accounts
- [ ] CORS origins updated to actual domain
- [ ] Database connection string updated
- [ ] Email SMTP credentials configured
- [ ] API key generated and configured
- [ ] Receipt encryption key generated (32-byte base64)
- [ ] Code committed to git
- [ ] Deployed to server
- [ ] Services restarted
- [ ] Health checks passing
- [ ] HTTPS working
- [ ] Security headers verified
- [ ] Admin login tested
- [ ] Default passwords changed
- [ ] Email sending tested
- [ ] Backup/restore tested

---

**Last Updated**: November 10, 2025  
**Next Security Review**: December 10, 2025
