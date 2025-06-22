@echo off
setlocal enabledelayedexpansion

echo ========================================
echo 🧪 빠른 부하테스트 (Prometheus 모니터링)
echo ========================================

:: 설정값들 (빠른 테스트용)
set GAMESERVER_PATH=..\..\Binary\Release\GameServer.exe
set DUMMYCLIENT_PATH=..\..\DummyClientCS\bin\Release\net9.0\DummyClientCS.exe
set TEST_CLIENTS=3
set TEST_DURATION=30
set MOVE_INTERVAL=250
set CHAT_INTERVAL=2000

echo 📊 테스트 설정:
echo    - 클라이언트: %TEST_CLIENTS%개
echo    - 테스트 시간: %TEST_DURATION%초
echo    - Move 간격: %MOVE_INTERVAL%ms
echo    - Chat 간격: %CHAT_INTERVAL%ms
echo.

:: 1단계: Docker 환경 시작 (빠른 시작)
echo 📦 Docker 환경 시작 중...
docker-compose -f ..\configs\docker-compose.yml up -d --quiet-pull 2>nul
set DOCKER_RESULT=%ERRORLEVEL%
if %DOCKER_RESULT% neq 0 (
    echo ❌ Docker 환경 시작 실패 (오류 코드: %DOCKER_RESULT%, 무시하고 계속)
) else (
    echo ✅ Docker 환경 시작됨
)
echo 🔍 Docker 시작 후 계속 진행...

timeout /t 3 /nobreak > nul

:: 2단계: GameServer 시작
echo.
echo 🎮 GameServer 시작 중...
echo 🔍 GameServer 경로 확인: %GAMESERVER_PATH%
if not exist "%GAMESERVER_PATH%" (
    echo ❌ GameServer 실행 파일을 찾을 수 없습니다: %GAMESERVER_PATH%
    echo    빌드가 필요할 수 있습니다.
    pause
    exit /b 1
)

echo 🔍 GameServer 실행 중...
start "GameServer" "%GAMESERVER_PATH%"
echo 🔍 GameServer 시작 완료, 3초 대기...
timeout /t 3 /nobreak > nul

:: 3단계: 클라이언트들 시작
echo.
echo 👥 %TEST_CLIENTS%개 클라이언트 시작 중...
echo 🔍 클라이언트 경로 확인: %DUMMYCLIENT_PATH%

:: 클라이언트 실행 파일 존재 확인
if not exist "%DUMMYCLIENT_PATH%" (
    echo ❌ DummyClient 실행 파일을 찾을 수 없습니다: %DUMMYCLIENT_PATH%
    echo    빌드가 필요할 수 있습니다.
    pause
    exit /b 1
)

echo ✅ 클라이언트 실행 파일 확인됨
echo.

:: 클라이언트 1 시작
echo   📡 클라이언트 1 시작 중 (HTTP 포트: 10100)...
echo   🔍 실행 명령: "%DUMMYCLIENT_PATH%" --client-id 1 --http-port 10100 --move-interval %MOVE_INTERVAL% --chat-interval %CHAT_INTERVAL%
start "DummyClient_1" "%DUMMYCLIENT_PATH%" --client-id 1 --http-port 10100 --move-interval %MOVE_INTERVAL% --chat-interval %CHAT_INTERVAL%
echo   ✅ 클라이언트 1 시작 명령 실행됨
timeout /t 1 /nobreak > nul

:: 클라이언트 2 시작
echo   📡 클라이언트 2 시작 중 (HTTP 포트: 10101)...
echo   🔍 실행 명령: "%DUMMYCLIENT_PATH%" --client-id 2 --http-port 10101 --move-interval %MOVE_INTERVAL% --chat-interval %CHAT_INTERVAL%
start "DummyClient_2" "%DUMMYCLIENT_PATH%" --client-id 2 --http-port 10101 --move-interval %MOVE_INTERVAL% --chat-interval %CHAT_INTERVAL%
echo   ✅ 클라이언트 2 시작 명령 실행됨
timeout /t 1 /nobreak > nul

:: 클라이언트 3 시작
echo   📡 클라이언트 3 시작 중 (HTTP 포트: 10102)...
echo   🔍 실행 명령: "%DUMMYCLIENT_PATH%" --client-id 3 --http-port 10102 --move-interval %MOVE_INTERVAL% --chat-interval %CHAT_INTERVAL%
start "DummyClient_3" "%DUMMYCLIENT_PATH%" --client-id 3 --http-port 10102 --move-interval %MOVE_INTERVAL% --chat-interval %CHAT_INTERVAL%
echo   ✅ 클라이언트 3 시작 명령 실행됨
timeout /t 1 /nobreak > nul

echo 🔍 모든 클라이언트 시작 명령 완료

:: 4단계: 모니터링 정보 표시
echo.
echo 🎉 클라이언트 시작 단계 완료!
echo 📋 다음 단계: 모니터링 정보 표시 중...
echo.
echo ✅ 모든 서비스 시작 완료!

:: 디버깅용 일시정지
echo 🔍 [디버그] 4단계 완료 - 아무 키나 누르면 계속...
pause
echo.
echo 📊 실시간 모니터링 주소:
echo    🎯 Grafana:           http://localhost:3000 (admin/admin)
echo    📈 Prometheus:        http://localhost:9090
echo    🔧 GameServer 메트릭: http://localhost:10001/metrics
echo    👤 Client 1 메트릭:   http://localhost:10100/metrics
echo    👤 Client 2 메트릭:   http://localhost:10101/metrics
echo    👤 Client 3 메트릭:   http://localhost:10102/metrics
echo.

:: 5단계: 테스트 실행
echo 🔍 [디버그] 5단계 시작 전 - 아무 키나 누르면 30초 테스트 시작...
pause

echo ⏱️ %TEST_DURATION%초간 테스트 실행 중...

:: for 루프 대신 단순한 방식으로 변경
echo   🌐 브라우저에서 Grafana 확인: http://localhost:3000
echo   📈 대시보드에서 실시간 지표를 확인해보세요!
echo   ⏰ 30초 동안 테스트 실행 중... (수동으로 모니터링하세요)
timeout /t %TEST_DURATION% /nobreak > nul
echo   ✅ 30초 테스트 완료!

:: 6단계: 결과 표시
echo.
echo ✅ 빠른 테스트 완료!
echo.
echo 📊 결과 확인:
echo    - Move 패킷: 클라이언트당 4 TPS 예상 (총 12 TPS)
echo    - Chat 패킷: 클라이언트당 0.5 TPS 예상 (총 1.5 TPS)
echo    - 예상 총 서버 TPS: ~13.5
echo.

:: 디버깅용 일시정지
echo 🔍 [디버그] 6단계 완료 - 아무 키나 누르면 계속...
pause

:: 7단계: 계속 실행 여부 확인
echo 🔄 계속 실행 옵션:
echo    1. 계속 실행 (수동 모니터링)
echo    2. 모든 프로세스 종료
echo    3. 클라이언트만 종료
echo.
set /p CONTINUE_OPTION="선택하세요 (1-3): "

if "%CONTINUE_OPTION%"=="1" (
    echo 📊 모든 서비스가 계속 실행 중입니다.
    echo    Grafana에서 실시간 모니터링을 계속하세요: http://localhost:3000
    echo    종료하려면 이 창을 닫거나 Ctrl+C를 누르세요.
    pause
) else if "%CONTINUE_OPTION%"=="2" (
    echo 🧹 모든 프로세스 종료 중...
    taskkill /f /im GameServer.exe 2>nul
    taskkill /f /im DummyClientCS.exe 2>nul
    docker-compose -f ..\configs\docker-compose.yml down --timeout 5 2>nul
    echo ✅ 정리 완료
) else (
    echo 🧹 클라이언트만 종료 중...
    taskkill /f /im DummyClientCS.exe 2>nul
    echo ✅ 클라이언트 정리 완료
    echo 📊 GameServer와 모니터링 시스템은 계속 실행 중
)

echo.
echo 🎉 빠른 테스트가 완료되었습니다!
echo    더 자세한 부하테스트는 gradual_load_test.bat을 실행하세요.
pause 