@echo off
echo ========================================
echo 🔄 Docker 모니터링 환경 재시작
echo ========================================

echo 📦 기존 Docker 환경 정리 중...
docker-compose -f ..\configs\docker-compose.yml down --volumes --remove-orphans
timeout /t 3 /nobreak > nul

echo 🧹 Docker 시스템 정리 중...
docker system prune -f
timeout /t 2 /nobreak > nul

echo 🚀 Docker 환경 새로 시작 중...
docker-compose -f ..\configs\docker-compose.yml up -d --force-recreate
timeout /t 10 /nobreak > nul

echo ✅ Docker 환경 재시작 완료!
echo.
echo 📊 접속 정보:
echo    - Prometheus: http://localhost:9090
echo    - Grafana:    http://localhost:3000 (admin/admin)
echo.
echo 💡 Grafana 접속 후 대시보드가 자동으로 로딩됩니다.
echo    약 30초 후에 데이터가 나타나기 시작합니다.
pause 