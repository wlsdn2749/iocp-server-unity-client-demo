@echo off
chcp 65001 > NUL
setlocal EnableDelayedExpansion

rem ANSI 이스케이프 문자 설정
for /f %%a in ('echo prompt $E^| cmd') do set "ESC=%%a"

rem 전역 변수
set "GAMESERVER_PID="
set "CLIENT_PID="
set "DOCKER_STARTED=false"
set GAMESERVER_PATH=..\..\Binary\Release\GameServer.exe
set DUMMYCLIENT_PATH=..\..\DummyClientCS\bin\Release\net9.0\DummyClientCS.exe

echo.
call :ECHO_CYAN "=================================================="
call :ECHO_CYAN "          🎯 Prometheus 성능 테스트 도구          "
call :ECHO_CYAN "=================================================="
echo.

rem 동시접속자 수 입력받기
:input_connections
call :ECHO_BLUE_NOLINE "동시접속자 몇 명으로 하시겠습니까? (기본값: 100): "
set /p user_connections=
if "%user_connections%"=="" set user_connections=100

rem 숫자 검증
echo %user_connections%| findstr /r "^[0-9][0-9]*$" >nul
if errorlevel 1 (
    call :ECHO_RED "❌ 숫자만 입력해주세요."
    goto input_connections
)

rem 연결 설정 파라미터
set batch_size=10
set interval_ms=100

echo.
call :ECHO_GREEN "📋 테스트 설정:"
echo    - 동시접속자 수: %user_connections%명
echo    - 연결 모드: 점진적 연결 (ConnectGradually)
echo    - 배치 크기: %batch_size%개씩
echo    - 연결 간격: %interval_ms%ms
echo    - Client 메트릭 포트: 10100
echo.

rem 확인 메시지
call :ECHO_YELLOW_NOLINE "테스트를 시작하시겠습니까? (Y/N): "
set /p confirm=
if /i not "%confirm%"=="y" if /i not "%confirm%"=="yes" (
    call :ECHO_RED "테스트가 취소되었습니다."
    pause
    exit /b
)

echo.
call :ECHO_CYAN "=================================================="
call :ECHO_CYAN "                🚀 테스트 시작                  "
call :ECHO_CYAN "=================================================="

rem 테스트 시작 알림
call :ECHO_YELLOW "💡 GameServer와 DummyClient가 별도 창에서 실행됩니다."
echo.

rem 프로세스 정리
call :ECHO_YELLOW "1. 기존 프로세스 정리 중..."
taskkill /f /im GameServer.exe 2>nul
taskkill /f /im DummyClientCS.exe 2>nul
timeout /t 2 /nobreak >nul

rem 현재 디렉토리 확인 및 설정
cd /d "%~dp0"
cd ..
set "LOADTEST_DIR=%CD%"

rem 1. Prometheus & Grafana 스택 시작
echo.
call :ECHO_YELLOW "2. Prometheus & Grafana 스택 시작 중..."
call :ECHO_BLUE "   작업 디렉토리: %LOADTEST_DIR%"
docker-compose -f configs/docker-compose.yml up -d
if errorlevel 1 (
    call :ECHO_RED "❌ Docker Compose 시작 실패"
    pause
    exit /b 1
)
set "DOCKER_STARTED=true"
call :ECHO_GREEN "   ✅ Prometheus: http://localhost:9090"
call :ECHO_GREEN "   ✅ Grafana: http://localhost:3000 (admin/admin)"

rem Docker 컨테이너 상태 확인
call :ECHO_BLUE "   Docker 컨테이너 상태 확인 중..."
timeout /t 5 /nobreak >nul
docker ps --format "table {{.Names}}\t{{.Status}}\t{{.Ports}}" --filter "name=grafana" --filter "name=prometheus"

rem 2. GameServer 시작 (별도 창)
echo.
call :ECHO_YELLOW "3. GameServer 시작 중..."
cd /d "%~dp0"

rem GameServer를 별도 창에서 시작 (경로 변수 사용)
start "🎮 GameServer" cmd /c ""%GAMESERVER_PATH%" & echo. & echo 🛑 GameServer가 종료되었습니다. 아무 키나 눌러 창을 닫으세요... & pause"
timeout /t 3 /nobreak >nul

rem GameServer PID 찾기
for /f "tokens=2" %%i in ('tasklist /fi "imagename eq GameServer.exe" /fo table /nh 2^>nul') do (
    set "GAMESERVER_PID=%%i"
    goto found_gameserver
)
:found_gameserver

if defined GAMESERVER_PID (
    call :ECHO_GREEN "   ✅ GameServer 시작됨 (PID: %GAMESERVER_PID%)"
) else (
    call :ECHO_RED "   ❌ GameServer 시작 실패"
    goto cleanup_and_exit
)

cd /d "%~dp0"
cd ..

rem 서버 시작 대기
call :ECHO_BLUE "   서버 초기화 대기 중... (3초)"
timeout /t 3 /nobreak >nul

rem 3. DummyClient 시작 (별도 창)
echo.
call :ECHO_YELLOW "4. DummyClient 시작 중 (%user_connections%개 연결)..."
cd /d "%~dp0"

rem DummyClient를 별도 창에서 시작 (경로 변수 사용)
start "🔌 DummyClient (%user_connections%개)" cmd /c ""%DUMMYCLIENT_PATH%" --connections %user_connections% --connection-mode gradually --batch-size %batch_size% --interval %interval_ms% --prometheus-port 10100 --gtest & echo. & echo 🛑 DummyClient가 종료되었습니다. 아무 키나 눌러 창을 닫으세요... & pause"
timeout /t 3 /nobreak >nul

rem DummyClient PID 찾기
for /f "tokens=2" %%i in ('tasklist /fi "imagename eq DummyClientCS.exe" /fo table /nh 2^>nul') do (
    set "CLIENT_PID=%%i"
    goto found_client
)
:found_client

if defined CLIENT_PID (
    call :ECHO_GREEN "   ✅ DummyClient 시작됨 (PID: %CLIENT_PID%)"
) else (
    call :ECHO_RED "   ❌ DummyClient 시작 실패"
    goto cleanup_and_exit
)

cd /d "%~dp0"
cd ..

echo.
call :ECHO_GREEN "=================================================="
call :ECHO_GREEN "              🎉 테스트 실행 완료              "
call :ECHO_GREEN "=================================================="
echo.

rem 테스트 상태 표시
call :ECHO_YELLOW "📄 테스트 실행 완료!"
call :ECHO_YELLOW "========================"
echo.
call :ECHO_GREEN "✅ GameServer 실행 중 (별도 창 - PID: %GAMESERVER_PID%)"
call :ECHO_GREEN "✅ DummyClient 실행 중 (별도 창 - PID: %CLIENT_PID%)"
call :ECHO_GREEN "✅ Docker 컨테이너 실행 중"
echo.
call :ECHO_CYAN "💡 사용 가이드:"
call :ECHO_BLUE "   📊 Grafana 대시보드: http://localhost:3000 (admin/admin)"
call :ECHO_BLUE "   📈 Prometheus: http://localhost:9090"
call :ECHO_BLUE "   🔍 GameServer 메트릭: http://localhost:10001/metrics"
call :ECHO_BLUE "   🔍 Client 메트릭: http://localhost:10100/metrics"
echo.
call :ECHO_YELLOW "💡 모니터링 팁:"
echo    - Grafana의 "JobQueue 대기 작업" 패널을 주시하세요
echo    - Push/Execute 비율이 차이나면 병목 발생 중입니다
echo    - 성능 테스트 중 대기 작업이 100개 이상 지속되면 주의하세요
echo.
call :ECHO_CYAN "🔧 문제 해결:"
call :ECHO_BLUE "   📋 Grafana 로그: docker logs grafana"
call :ECHO_BLUE "   📋 Prometheus 로그: docker logs prometheus"
call :ECHO_BLUE "   📋 컨테이너 상태: docker ps"
echo.
call :ECHO_RED "🛑 테스트를 완전히 종료하려면 아무 키나 누르세요..."
call :ECHO_RED "   (모든 프로세스와 Docker 컨테이너가 정리됩니다)"

rem 사용자 입력 대기
pause >nul

:cleanup_and_exit
echo.
call :ECHO_RED "🛑 모든 프로세스 정리 중..."

if defined GAMESERVER_PID (
    taskkill /f /pid %GAMESERVER_PID% 2>nul
    call :ECHO_GREEN "   ✅ GameServer 종료 (PID: %GAMESERVER_PID%)"
)

if defined CLIENT_PID (
    taskkill /f /pid %CLIENT_PID% 2>nul  
    call :ECHO_GREEN "   ✅ DummyClient 종료 (PID: %CLIENT_PID%)"
)

if "%DOCKER_STARTED%"=="true" (
    call :ECHO_YELLOW "   Docker 컨테이너 정리 중..."
    cd /d "%LOADTEST_DIR%"
    docker-compose -f configs/docker-compose.yml down >nul 2>&1
    call :ECHO_GREEN "   ✅ Docker 컨테이너 정리 완료"
)

echo.
call :ECHO_GREEN "🎉 모든 프로세스가 정리되었습니다."
call :ECHO_BLUE "감사합니다!"
timeout /t 3 /nobreak >nul
exit /b

rem ===== 색상 함수 정의 =====
:ECHO_RED
echo %ESC%[31m%~1%ESC%[0m
exit /b

:ECHO_GREEN
echo %ESC%[32m%~1%ESC%[0m
exit /b

:ECHO_YELLOW
echo %ESC%[33m%~1%ESC%[0m
exit /b

:ECHO_BLUE
echo %ESC%[34m%~1%ESC%[0m
exit /b

:ECHO_MAGENTA
echo %ESC%[35m%~1%ESC%[0m
exit /b

:ECHO_CYAN
echo %ESC%[36m%~1%ESC%[0m
exit /b

:ECHO_WHITE
echo %ESC%[37m%~1%ESC%[0m
exit /b

rem 줄바꿈 없는 버전
:ECHO_RED_NOLINE
set /p "=%ESC%[31m%~1%ESC%[0m" <nul
exit /b

:ECHO_GREEN_NOLINE
set /p "=%ESC%[32m%~1%ESC%[0m" <nul
exit /b

:ECHO_YELLOW_NOLINE
set /p "=%ESC%[33m%~1%ESC%[0m" <nul
exit /b

:ECHO_BLUE_NOLINE
set /p "=%ESC%[34m%~1%ESC%[0m" <nul
exit /b

:ECHO_CYAN_NOLINE
set /p "=%ESC%[36m%~1%ESC%[0m" <nul
exit /b 