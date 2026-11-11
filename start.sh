#!/usr/bin/env bash
# Menjalankan seluruh HelpDeskPro (SQL Server + API + Web) dengan satu perintah.
# Pemakaian: ./start.sh   (Ctrl+C untuk menghentikan API & Web)
set -euo pipefail
cd "$(dirname "$0")"

[ -f .env ] || { cp .env.example .env; echo ".env dibuat dari .env.example"; }

echo "1/3 Menyalakan SQL Server (Docker)..."
docker compose up -d
echo "    Menunggu SQL Server siap..."
for _ in $(seq 1 60); do
  [ "$(docker inspect -f '{{.State.Health.Status}}' helpdeskpro-sqlserver 2>/dev/null)" = "healthy" ] && break
  sleep 2
done

mkdir -p logs
echo "2/3 Menjalankan API  -> http://localhost:5017/swagger  (log: logs/api.out)"
dotnet run --project src/HelpDeskPro.Api --launch-profile http > logs/api.out 2>&1 &
API_PID=$!
echo "3/3 Menjalankan Web  -> http://localhost:5015          (log: logs/web.out)"
dotnet run --project src/HelpDeskPro.Web --launch-profile http > logs/web.out 2>&1 &
WEB_PID=$!
trap 'echo; echo "Menghentikan API & Web..."; kill $API_PID $WEB_PID 2>/dev/null; wait 2>/dev/null' INT TERM EXIT

echo "    Menunggu aplikasi siap..."
for _ in $(seq 1 90); do
  curl -fs -o /dev/null http://localhost:5015/login && curl -fs -o /dev/null http://localhost:5017/swagger/index.html && break
  sleep 2
done

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
