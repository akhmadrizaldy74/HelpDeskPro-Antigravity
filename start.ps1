# Menjalankan seluruh HelpDeskPro (SQL Server + API + Web) dengan satu perintah.
# Pemakaian (PowerShell, dari folder repo):  .\start.ps1
# Jika muncul error execution policy:      powershell -ExecutionPolicy Bypass -File .\start.ps1

$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot

if (-not (Test-Path '.env')) {
    Copy-Item '.env.example' '.env'
    Write-Host '.env dibuat dari .env.example' -ForegroundColor Yellow
}

Write-Host '1/3 Menyalakan SQL Server (Docker)...' -ForegroundColor Cyan
docker compose up -d
if ($LASTEXITCODE -ne 0) { throw 'Docker gagal dijalankan. Pastikan Docker Desktop sudah menyala.' }

Write-Host '    Menunggu SQL Server siap...'
for ($i = 0; $i -lt 60; $i++) {
    $status = docker inspect -f '{{.State.Health.Status}}' helpdeskpro-sqlserver 2>$null
    if ($status -eq 'healthy') { break }
    Start-Sleep -Seconds 2
}

Write-Host '2/3 Menjalankan API  -> http://localhost:5017/swagger' -ForegroundColor Cyan
Start-Process powershell -ArgumentList '-NoExit', '-Command', "Set-Location '$PSScriptRoot'; `$host.UI.RawUI.WindowTitle='HelpDeskPro API'; dotnet run --project src/HelpDeskPro.Api --launch-profile http"

Write-Host '3/3 Menjalankan Web  -> http://localhost:5015' -ForegroundColor Cyan
Start-Process powershell -ArgumentList '-NoExit', '-Command', "Set-Location '$PSScriptRoot'; `$host.UI.RawUI.WindowTitle='HelpDeskPro Web'; dotnet run --project src/HelpDeskPro.Web --launch-profile http"

Write-Host '    Menunggu aplikasi siap...'
for ($i = 0; $i -lt 90; $i++) {
    try {
        Invoke-WebRequest -Uri 'http://localhost:5015/login' -UseBasicParsing -TimeoutSec 2 | Out-Null
        Invoke-WebRequest -Uri 'http://localhost:5017/swagger/index.html' -UseBasicParsing -TimeoutSec 2 | Out-Null
        break
    } catch { Start-Sleep -Seconds 2 }
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
