# Menjalankan seluruh HelpDeskPro (SQL Server + API + Web) dengan satu perintah.
# Pemakaian (PowerShell, dari folder repo):  .\start.ps1
# Jika muncul error execution policy:      powershell -ExecutionPolicy Bypass -File .\start.ps1

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

$apiUrl = 'http://localhost:5017/swagger/index.html'
$webUrl = 'http://localhost:5015/login'

function Test-Url([string]$url) {
    try {
        Invoke-WebRequest -Uri $url -UseBasicParsing -TimeoutSec 3 | Out-Null
        return $true
    } catch {
        return $false
    }
}

function Wait-Url([string]$url, [int]$timeoutSeconds) {
    $deadline = (Get-Date).AddSeconds($timeoutSeconds)
    while ((Get-Date) -lt $deadline) {
        if (Test-Url $url) { return $true }
        Start-Sleep -Seconds 2
    }
    return $false
}

function Test-PortInUse([int]$port) {
    return [bool](Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue)
}

function Stop-Port([int]$port) {
    Get-NetTCPConnection -LocalPort $port -State Listen -ErrorAction SilentlyContinue |
        ForEach-Object { Stop-Process -Id $_.OwningProcess -Force -ErrorAction SilentlyContinue }
    Start-Sleep -Seconds 1
}

if (-not (Test-Path '.env')) {
    Copy-Item '.env.example' '.env'
    Write-Host '.env dibuat dari .env.example' -ForegroundColor Yellow
}

# ---------------------------------------------------------------- 1. Database
Write-Host '1/4 Menyalakan SQL Server (Docker)...' -ForegroundColor Cyan
docker compose up -d
if ($LASTEXITCODE -ne 0) { throw 'Docker gagal dijalankan. Pastikan Docker Desktop sudah menyala.' }

Write-Host '    Menunggu SQL Server siap...'
for ($i = 0; $i -lt 60; $i++) {
    $status = docker inspect -f '{{.State.Health.Status}}' helpdeskpro-sqlserver 2>$null
    if ($status -eq 'healthy') { break }
    Start-Sleep -Seconds 2
}

# ---------------------------------------------------------------- 2. Build (sekali)
# API dan Web dibangun SEKALI di sini. Jika keduanya di-build bersamaan lewat
# "dotnet run", file project bersama (Application/Domain) bisa terkunci di Windows
# sehingga salah satu gagal start.
Write-Host '2/4 Build solution...' -ForegroundColor Cyan

# Hentikan API/Web lama agar file .dll tidak terkunci saat build.
if (Test-PortInUse 5017) { Write-Host '    Menghentikan API lama di port 5017...'; Stop-Port 5017 }
if (Test-PortInUse 5015) { Write-Host '    Menghentikan Web lama di port 5015...'; Stop-Port 5015 }

dotnet build HelpDeskPro.sln -v quiet -nologo
if ($LASTEXITCODE -ne 0) { throw 'Build gagal. Lihat pesan error di atas.' }

# ---------------------------------------------------------------- 3. API
Write-Host '3/4 Menjalankan API  -> http://localhost:5017/swagger' -ForegroundColor Cyan
Start-Process powershell -ArgumentList '-NoExit', '-Command', "Set-Location '$PSScriptRoot'; `$host.UI.RawUI.WindowTitle='HelpDeskPro API'; dotnet run --no-build --project src/HelpDeskPro.Api --launch-profile http"

Write-Host '    Menunggu API siap (migration & seeding pertama kali bisa ~1 menit)...'
if (-not (Wait-Url $apiUrl 180)) {
    Write-Host ''
    Write-Host 'API GAGAL START. Lihat pesan error di jendela "HelpDeskPro API".' -ForegroundColor Red
    Write-Host 'Penyebab umum: Docker/SQL Server belum siap, atau password di .env tidak cocok.' -ForegroundColor Red
    exit 1
}

# ---------------------------------------------------------------- 4. Web
Write-Host '4/4 Menjalankan Web  -> http://localhost:5015' -ForegroundColor Cyan
Start-Process powershell -ArgumentList '-NoExit', '-Command', "Set-Location '$PSScriptRoot'; `$host.UI.RawUI.WindowTitle='HelpDeskPro Web'; dotnet run --no-build --project src/HelpDeskPro.Web --launch-profile http"

if (-not (Wait-Url $webUrl 120)) {
    Write-Host ''
    Write-Host 'WEB GAGAL START. Lihat pesan error di jendela "HelpDeskPro Web".' -ForegroundColor Red
    exit 1
}

Start-Process 'http://localhost:5015'

Write-Host ''
Write-Host 'Siap! Login dengan salah satu akun (password: Dev@Passw0rd123!):' -ForegroundColor Green
Write-Host '  Admin      : admin@helpdeskpro.local'
Write-Host '  Supervisor : supervisor.it@helpdeskpro.local'
Write-Host '  Agent      : agent.agus@helpdeskpro.local'
Write-Host '  Requester  : requester.bambang@helpdeskpro.local'
Write-Host 'Swagger: http://localhost:5017/swagger (Execute /api/Auth/login, token terpasang otomatis)'
Write-Host 'Untuk berhenti: tutup jendela "HelpDeskPro API" & "HelpDeskPro Web", lalu: docker compose stop'
