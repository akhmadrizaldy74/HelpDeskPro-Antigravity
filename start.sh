#!/usr/bin/env bash
# Menjalankan seluruh HelpDeskPro (SQL Server + API + Web) dengan satu perintah.
# Pemakaian: ./start.sh   (Ctrl+C untuk menghentikan API & Web)
set -euo pipefail
cd "$(dirname "$0")"

[ -f .env ] || { cp .env.example .env; echo ".env dibuat dari .env.example"; }

echo "1/4 Menyalakan SQL Server (Docker)..."
docker compose up -d
echo "    Menunggu SQL Server siap..."
for _ in $(seq 1 60); do
  [ "$(docker inspect -f '{{.State.Health.Status}}' helpdeskpro-sqlserver 2>/dev/null)" = "healthy" ] && break
  sleep 2
done

mkdir -p logs
echo "2/4 Build solution (sekali, agar API & Web tidak berebut file saat build)..."
dotnet build HelpDeskPro.sln -v quiet -nologo

wait_url() { # url timeout_detik
  for _ in $(seq 1 $(( $2 / 2 ))); do curl -fs -o /dev/null "$1" && return 0; sleep 2; done; return 1
}

echo "3/4 Menjalankan API  -> http://localhost:5017/swagger  (log: logs/api.out)"
dotnet run --no-build --project src/HelpDeskPro.Api --launch-profile http > logs/api.out 2>&1 &
API_PID=$!
trap 'echo; echo "Menghentikan API & Web..."; kill $API_PID ${WEB_PID:-} 2>/dev/null; wait 2>/dev/null' INT TERM EXIT
echo "    Menunggu API siap..."
wait_url http://localhost:5017/swagger/index.html 180 || { echo "API GAGAL START. Lihat logs/api.out"; tail -20 logs/api.out; exit 1; }

echo "4/4 Menjalankan Web  -> http://localhost:5015          (log: logs/web.out)"
dotnet run --no-build --project src/HelpDeskPro.Web --launch-profile http > logs/web.out 2>&1 &
WEB_PID=$!
wait_url http://localhost:5015/login 120 || { echo "WEB GAGAL START. Lihat logs/web.out"; tail -20 logs/web.out; exit 1; }

cat <<'MSG'

Siap! Buka http://localhost:5015 lalu login (password: Dev@Passw0rd123!):
  Admin      : admin@helpdeskpro.local
  Supervisor : supervisor.it@helpdeskpro.local
  Agent      : agent.agus@helpdeskpro.local
  Requester  : requester.bambang@helpdeskpro.local
Swagger: http://localhost:5017/swagger (Execute /api/Auth/login, token terpasang otomatis)
Tekan Ctrl+C untuk berhenti.
MSG
wait
