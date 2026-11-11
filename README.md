# HelpDeskPro

Sistem manajemen tiket dan help desk internal IT berbasis .NET 8 dengan Clean Architecture, ASP.NET Core Web API, dan Blazor Web App (Interactive Server).

---

## Arsitektur Solusi

```text
HelpDeskPro/
├── src/
│   ├── HelpDeskPro.Domain          # Entity, Enum, Value Objects, Domain Exceptions
│   ├── HelpDeskPro.Application     # Commands, Queries, DTOs, Validators, Interfaces
│   ├── HelpDeskPro.Infrastructure  # EF Core DbContext, Configurations, Migrations, External Services
│   ├── HelpDeskPro.Api             # ASP.NET Core Web API (Controllers tipis)
│   └── HelpDeskPro.Web             # Blazor Web App (Interactive Server UI)
├── tests/
│   ├── HelpDeskPro.UnitTests       # Unit tests (xUnit, FluentAssertions, NSubstitute)
│   └── HelpDeskPro.IntegrationTests# Integration tests (WebApplicationFactory, Testcontainers)
├── .editorconfig                   # Aturan penamaan dan coding standard Roslyn
├── Directory.Build.props           # Pengaturan build terpusat (net8.0, nullable, warnings as errors)
├── Directory.Packages.props        # Central Package Management (CPM)
├── docker-compose.yml              # SQL Server 2022
└── .github/workflows/ci.yml        # CI pipeline GitHub Actions
```

---

## Prasyarat

- [.NET 8.0 SDK](https://dotnet.microsoft.com/download/dotnet/8.0) atau lebih baru
- [Docker](https://www.docker.com/) & Docker Compose
- [dotnet-ef CLI tool](https://learn.microsoft.com/ef/core/cli/dotnet) (opsional untuk migration): `dotnet tool restore` (versi 8.0.11 sesuai `.config/dotnet-tools.json`)

---

## Cara Cepat (satu perintah)

Prasyarat: Docker Desktop menyala dan .NET 8 SDK terpasang.

**Windows (PowerShell):**
```powershell
.\start.ps1
```
Jika diblokir execution policy: `powershell -ExecutionPolicy Bypass -File .\start.ps1`

**Linux / macOS:**
```bash
./start.sh
```

Skrip akan membuat `.env`, menyalakan SQL Server, menjalankan API (migration + seeding otomatis) dan Web, lalu membuka `http://localhost:5015`. Tinggal login dengan akun di bawah (password `Dev@Passw0rd123!`).

Di Swagger (`http://localhost:5017/swagger`), contoh body **POST /api/Auth/login** sudah terisi akun Admin. Cukup klik **Try it out** → **Execute**, token otomatis terpasang di tombol **Authorize** (tetap tersimpan saat refresh), dan endpoint lain langsung bisa dicoba.

---

## Panduan Menjalankan Lokal (manual)

### 1. Jalankan Database SQL Server (Docker Compose)

Salin konfigurasi environment dari contoh:
```bash
cp .env.example .env
```

Jalankan container SQL Server 2022 di background:
```bash
docker compose up -d
```
Container SQL Server akan aktif di port `1433`. Password SA pada `.env.example` (`HelpDesk#2026`) sama dengan connection string default di `appsettings.json`; jika Anda menggantinya, ubah juga connection string (langkah 2).

### 2. Konfigurasi User Secrets (API) — opsional

Sesuai aturan keamanan di `.agent/rules/coding-standard.md`, credential dan string koneksi database lokal tidak disimpan di `appsettings.json`, melainkan menggunakan .NET User Secrets:

```bash
# Inisialisasi User Secrets pada project API
dotnet user-secrets init --project src/HelpDeskPro.Api

# Set connection string database lokal
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=HelpDeskProDb;User Id=sa;Password=HelpDesk#2026;TrustServerCertificate=True;" --project src/HelpDeskPro.Api
```

### 3. Jalankan Database Migration

Terapkan migration EF Core ke database SQL Server:
```bash
dotnet ef database update --project src/HelpDeskPro.Infrastructure --startup-project src/HelpDeskPro.Api
```

### 4. Menjalankan Aplikasi

Jalankan API dan Web Blazor di dua terminal terpisah:

**Terminal 1 — Backend API:**
```bash
dotnet run --project src/HelpDeskPro.Api
```
Swagger UI tersedia di `http://localhost:5017/swagger`. Pada environment Development, migration dan seeding data dijalankan otomatis saat API start.

**Terminal 2 — Frontend Web (Blazor Interactive Server):**
```bash
dotnet run --project src/HelpDeskPro.Web
```
Aplikasi Web dapat diakses di `http://localhost:5015`. Web memanggil API melalui `ApiSettings:BaseUrl` (default `http://localhost:5017`), jadi pastikan API sudah berjalan.

Akun hasil seeding (password: `Dev@Passw0rd123!`):

| Role | Email |
|------|-------|
| Admin | `admin@helpdeskpro.local` |
| Supervisor | `supervisor.it@helpdeskpro.local` |
| Agent | `agent.agus@helpdeskpro.local` |
| Requester | `requester.bambang@helpdeskpro.local` |

---

## Menjalankan Pengujian

Jalankan seluruh test suite (Unit & Integration tests):
```bash
dotnet test
```

Untuk menjalankan dengan laporan coverage:
```bash
dotnet test --collect:"XPlat Code Coverage"
```

