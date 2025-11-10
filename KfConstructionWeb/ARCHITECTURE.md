# KfConstruction Architecture

## Projects
- **KfConstructionWeb**: MVC UI + Razor Pages (Identity). User/admin flows.
- **KfConstructionAPI**: External API with API-key auth.

## Key Folders (Web)
- `Controllers/`: Public MVC controllers
- `Areas/Admin/Controllers/`: Admin MVC controllers `[Authorize(Roles="Admin")]`
- `Areas/Identity/Pages/`: Authentication (Razor Pages)
- `Services/Interfaces/`: All service interfaces
- `Services/`: Service implementations
- `Models/`: Domain models + DTOs/ViewModels/Exceptions/Performance/ReceiptOcr
- `Middleware/`: Performance tracking, account lock, maintenance
- `Data/`: ApplicationDbContext & migrations
- `wwwroot/`: Static assets (css/js/uploads)

## Rules
- Interface-first: Controllers depend on `Services/Interfaces` only
- No EF entities in views or APIs. Use ViewModels/DTOs
- One DI registration per interface
- Extract reusable domain types to `Models/*`

## Run Locally
```powershell
.\run-balanced.ps1
```
Web: https://localhost:7085  
API: https://localhost:7136/swagger

## Migrations
```powershell
dotnet ef migrations add <Name> -p KfConstructionWeb -s KfConstructionWeb
dotnet ef database update -p KfConstructionWeb -s KfConstructionWeb
```

## Add Feature
1. Define domain model in `Models/<Group>/`
2. Create interface in `Services/Interfaces` + implementation in `Services`
3. Register in Program.cs
4. Add controller + view + ViewModel (or DTO for API)
5. Add validations

## Security
- HTTPS + HSTS in production
- API: API-key middleware
- Files: AES-256-GCM format `[nonce(12)][tag(16)][ciphertext]`
- OCR: Azure Form Recognizer → domain `OcrResult`

## Troubleshooting
See `TROUBLESHOOTING.md` for quick fixes.
