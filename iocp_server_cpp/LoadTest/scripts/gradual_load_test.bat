@echo off
:: 변수 확장 문제를 피하기 위해 enabledelayedexpansion 제거
setlocal

echo ========================================
echo 🚀 점진적 부하테스트 + Prometheus 모니터링
echo ========================================

:: 설정값들
set GAMESERVER_PATH=..\..\Binary\Release\GameServer.exe
set DUMMYCLIENT_PATH=..\..\DummyClientCS\bin\Release\net9.0\DummyClientCS.exe
set MAX_CLIENTS=30
set STEP_DURATION=10
set RAMP_UP_DELAY=1
set MOVE_INTERVAL=250
set CHAT_INTERVAL=2000

:: 1단계: Docker 환경 시작
echo.
echo 📦 Docker 환경 시작 중...
docker-compose -f ..\configs\docker-compose.yml up -d
if errorlevel 1 (
    echo ❌ Docker 환경 시작 실패
    pause
    exit /b 1
)

:: 2단계: 모니터링 서비스 대기
echo.
echo ⏱️ 모니터링 서비스 준비 대기 중...
timeout /t 10 /nobreak > nul

:: 3단계: GameServer 시작
echo.
echo 🎮 GameServer 시작 중...
if not exist "%GAMESERVER_PATH%" (
    echo ❌ GameServer 실행 파일을 찾을 수 없습니다: %GAMESERVER_PATH%
    echo    다음 명령으로 빌드해주세요:
    echo    cd ..\GameServer
    echo    msbuild GameServer.vcxproj /p:Configuration=Release
    pause
    exit /b 1
)

start "GameServer" "%GAMESERVER_PATH%"
timeout /t 5 /nobreak > nul

:: 4단계: 점진적 클라이언트 증가
echo.
echo 👥 점진적 클라이언트 증가 시작...
echo    - 최대 클라이언트: %MAX_CLIENTS%개
echo    - 클라이언트 추가 간격: %RAMP_UP_DELAY%초
echo    - 각 단계 측정 시간: %STEP_DURATION%초
echo    - Move 간격: %MOVE_INTERVAL%ms
echo    - Chat 간격: %CHAT_INTERVAL%ms
echo.

echo 📊 실시간 모니터링 주소:
echo    - 🎯 Grafana: http://localhost:3000 (admin/admin)
echo    - 📈 Prometheus: http://localhost:9090
echo    - 🔧 GameServer 메트릭: http://localhost:10001/metrics
echo.

:: 1개씩 클라이언트 추가 (명시적 방식 - 재귀 호출 없음)
echo 🔄 클라이언트 추가 시작...

:: 클라이언트 1-5 시작
echo.
echo 🎯 클라이언트 1-5 추가 중...
call :StartClient 1
call :StartClient 2
call :StartClient 3
call :StartClient 4
call :StartClient 5
call :MeasurePerformance 5

:: 클라이언트 6-10 시작
echo.
echo 🎯 클라이언트 6-10 추가 중...
call :StartClient 6
call :StartClient 7
call :StartClient 8
call :StartClient 9
call :StartClient 10
call :MeasurePerformance 10

:: 클라이언트 11-15 시작
echo.
echo 🎯 클라이언트 11-15 추가 중...
call :StartClient 11
call :StartClient 12
call :StartClient 13
call :StartClient 14
call :StartClient 15
call :MeasurePerformance 15

:: 클라이언트 16-20 시작
echo.
echo 🎯 클라이언트 16-20 추가 중...
call :StartClient 16
call :StartClient 17
call :StartClient 18
call :StartClient 19
call :StartClient 20
call :MeasurePerformance 20

:: 클라이언트 21-25 시작
echo.
echo 🎯 클라이언트 21-25 추가 중...
call :StartClient 21
call :StartClient 22
call :StartClient 23
call :StartClient 24
call :StartClient 25
call :MeasurePerformance 25

:: 클라이언트 26-30 시작
echo.
echo 🎯 클라이언트 26-30 추가 중...
call :StartClient 26
call :StartClient 27
call :StartClient 28
call :StartClient 29
call :StartClient 30
call :MeasurePerformance 30

goto END_TEST

:: 개별 클라이언트 시작 (단순한 서브루틴)
:StartClient
set CLIENT_NUM=%1
set /a HTTP_PORT=10099+%CLIENT_NUM%

echo   📡 [%CLIENT_NUM%/30] 클라이언트 %CLIENT_NUM% 시작 중 (HTTP 포트: %HTTP_PORT%)...
start "DummyClient_%CLIENT_NUM%" "%DUMMYCLIENT_PATH%" --client-id %CLIENT_NUM% --http-port %HTTP_PORT% --move-interval %MOVE_INTERVAL% --chat-interval %CHAT_INTERVAL%

timeout /t %RAMP_UP_DELAY% /nobreak > nul
goto :eof

:: 성능 측정
:MeasurePerformance
set CLIENT_COUNT=%1
set /a MOVE_TPS=%CLIENT_COUNT%*4
set /a CHAT_TPS=%CLIENT_COUNT%/2
set /a TOTAL_TPS=%MOVE_TPS%+%CHAT_TPS%

echo.
echo   ⏱️ %CLIENT_COUNT%개 클라이언트 상태에서 %STEP_DURATION%초간 성능 측정 중...
echo   📊 현재 예상 TPS: Move=%MOVE_TPS% + Chat=%CHAT_TPS% = 총 %TOTAL_TPS%
echo   📈 Grafana: http://localhost:3000에서 실시간 지표 확인

timeout /t %STEP_DURATION% /nobreak > nul

echo   ✅ %CLIENT_COUNT%개 클라이언트 측정 완료
echo.
goto :eof

:END_TEST
echo.
echo ✅ 부하테스트 완료!
echo.
echo 📊 결과 확인:
echo    - Grafana 대시보드: http://localhost:3000
echo    - Prometheus 메트릭: http://localhost:9090
echo    - 최대 클라이언트: %CURRENT_CLIENTS%개
echo.

:: 정리 옵션 제공
echo 🧹 정리 옵션:
echo    1. 모든 프로세스 종료 (추천)
echo    2. 클라이언트만 종료
echo    3. 그대로 두기 (수동 모니터링)
echo.
set /p CLEANUP_OPTION="선택하세요 (1-3): "

if "%CLEANUP_OPTION%"=="1" (
    echo 🧹 모든 프로세스 종료 중...
    taskkill /f /im GameServer.exe 2>nul
    taskkill /f /im DummyClientCS.exe 2>nul
    docker-compose -f ..\configs\docker-compose.yml down
    echo ✅ 정리 완료
) else if "%CLEANUP_OPTION%"=="2" (
    echo 🧹 클라이언트만 종료 중...
    taskkill /f /im DummyClientCS.exe 2>nul
    echo ✅ 클라이언트 정리 완료
    echo 📊 Grafana와 GameServer는 계속 실행 중
) else (
    echo 📊 모든 서비스가 계속 실행 중입니다
    echo    수동으로 종료하려면 다음 명령을 사용하세요:
    echo    taskkill /f /im GameServer.exe
    echo    taskkill /f /im DummyClientCS.exe
    echo    docker-compose -f ..\configs\docker-compose.yml down
)

echo.
echo 🎉 부하테스트가 완료되었습니다!
pause 