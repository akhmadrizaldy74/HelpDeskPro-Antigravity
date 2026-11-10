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
- [dotnet-ef CLI tool](https://learn.microsoft.com/ef/core/cli/dotnet) (opsional untuk migration): `dotnet tool install --global dotnet-ef`

---

## Panduan Menjalankan Lokal

### 1. Jalankan Database SQL Server (Docker Compose)

Salin konfigurasi environment dari contoh:
```bash
cp .env.example .env
```

Jalankan container SQL Server 2022 di background:
```bash
docker compose up -d
```
Container SQL Server akan aktif di port `1433`.

### 2. Konfigurasi User Secrets (API)

Sesuai aturan keamanan di `.agent/rules/coding-standard.md`, credential dan string koneksi database lokal tidak disimpan di `appsettings.json`, melainkan menggunakan .NET User Secrets:

```bash
# Inisialisasi User Secrets pada project API
dotnet user-secrets init --project src/HelpDeskPro.Api

# Set connection string database lokal
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Server=localhost,1433;Database=HelpDeskProDb;User Id=sa;Password=YourStrong@Passw0rd!;TrustServerCertificate=True;" --project src/HelpDeskPro.Api
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
Swagger UI tersedia di `https://localhost:7087/swagger` (atau port HTTPS yang dialokasikan).

**Terminal 2 — Frontend Web (Blazor Interactive Server):**
```bash
dotnet run --project src/HelpDeskPro.Web
```
Aplikasi Web dapat diakses di browser melalui URL yang ditampilkan (contoh: `https://localhost:7214`).

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

