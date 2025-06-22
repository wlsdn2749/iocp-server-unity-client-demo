@echo off
setlocal enabledelayedexpansion

echo ========================================
echo 🧪 간단한 테스트 (디버깅용)
echo ========================================

:: 설정값들
set GAMESERVER_PATH=..\..\Binary\Release\GameServer.exe
set DUMMYCLIENT_PATH=..\..\DummyClientCS\bin\Release\net9.0\DummyClientCS.exe
set TEST_CLIENTS=1

echo 📊 테스트 설정:
echo    - 클라이언트: %TEST_CLIENTS%개
echo    - GameServer 경로: %GAMESERVER_PATH%
echo    - Client 경로: %DUMMYCLIENT_PATH%
echo.

:: 1단계: 파일 존재 확인
echo 🔍 1단계: 파일 존재 확인
if not exist "%GAMESERVER_PATH%" (
    echo ❌ GameServer 없음: %GAMESERVER_PATH%
    pause
    exit /b 1
) else (
    echo ✅ GameServer 확인됨
)

if not exist "%DUMMYCLIENT_PATH%" (
    echo ❌ DummyClient 없음: %DUMMYCLIENT_PATH%
    pause
    exit /b 1
) else (
    echo ✅ DummyClient 확인됨
)

echo.

:: 2단계: GameServer 시작
echo 🔍 2단계: GameServer 시작
echo 🎮 GameServer 시작 중...
start "GameServer" "%GAMESERVER_PATH%"
echo ✅ GameServer 시작 명령 실행됨
timeout /t 3 /nobreak > nul
echo.

:: 3단계: 클라이언트 시작
echo 🔍 3단계: 클라이언트 시작
echo 👥 클라이언트 1개 시작 중...
echo 📡 실행할 명령: "%DUMMYCLIENT_PATH%" --client-id 1 --http-port 10100 --move-interval 250 --chat-interval 2000
start "DummyClient_1" "%DUMMYCLIENT_PATH%" --client-id 1 --http-port 10100 --move-interval 250 --chat-interval 2000
echo ✅ 클라이언트 시작 명령 실행됨
echo.

:: 4단계: 완료
echo 🔍 4단계: 완료
echo ✅ 모든 프로세스 시작 완료!
echo.
echo 📊 확인사항:
echo    - GameServer: 새 창에서 실행 중
echo    - DummyClient: 새 창에서 실행 중
echo    - 메트릭: http://localhost:10001/metrics (GameServer)
echo    - 메트릭: http://localhost:10100/metrics (Client)
echo.

echo 💡 이 창을 닫으면 테스트가 종료됩니다.
echo    프로세스를 계속 실행하려면 아무 키나 누르세요...
pause

echo 🧹 정리 중...
taskkill /f /im GameServer.exe 2>nul
taskkill /f /im DummyClientCS.exe 2>nul
echo ✅ 정리 완료 