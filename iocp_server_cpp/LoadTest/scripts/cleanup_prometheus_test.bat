@echo off
chcp 65001 > NUL

:: 색상 코드 정의
set "GREEN=[32m"
set "YELLOW=[33m"
set "RED=[31m"
set "BLUE=[34m"
set "RESET=[0m"

echo.
echo %RED%🛑 Prometheus 테스트 환경 정리%RESET%
echo %RED%================================%RESET%
echo.

:: 현재 디렉토리 설정
cd /d "%~dp0"
cd ..

echo %YELLOW%1. 실행 중인 프로세스 종료 중...%RESET%
taskkill /f /im GameServer.exe 2>nul
taskkill /f /im DummyClientCS.exe 2>nul
echo %GREEN%   ✅ 게임 프로세스 종료 완료%RESET%

echo.
echo %YELLOW%2. Docker 컨테이너 정리 중...%RESET%
docker-compose -f configs/docker-compose.yml down
if errorlevel 1 (
    echo %RED%   ❌ Docker Compose 정리 실패%RESET%
) else (
    echo %GREEN%   ✅ Prometheus & Grafana 컨테이너 정리 완료%RESET%
)

echo.
echo %YELLOW%3. 임시 파일 정리 중...%RESET%
del client_stats_*.json 2>nul
echo %GREEN%   ✅ 클라이언트 통계 파일 정리 완료%RESET%

echo.
echo %GREEN%🎉 정리 완료!%RESET%
echo.
pause 