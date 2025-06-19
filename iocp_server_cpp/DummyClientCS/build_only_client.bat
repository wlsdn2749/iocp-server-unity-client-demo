@echo off
chcp 65001
echo.
echo 🎯 DummyClientCS 단독 빌드
echo ===============================================

cd /d "%~dp0"

echo 📦 DummyClientCS.csproj만 빌드 중...
dotnet build DummyClientCS.csproj

if errorlevel 1 (
    echo ❌ DummyClientCS 빌드 실패
    echo    .NET 9.0 SDK가 설치되어 있는지 확인하세요.
    pause
    exit /b 1
)

echo.
echo ✅ DummyClientCS 빌드 완료!
echo ===============================================

if exist bin/Debug/net9.0/DummyClientCS.exe (
    echo 📄 빌드된 파일: bin/Debug/net9.0/DummyClientCS.exe
) else if exist bin/Release/net9.0/DummyClientCS.exe (
    echo 📄 빌드된 파일: bin/Release/net9.0/DummyClientCS.exe
) else (
    echo ⚠️  실행 파일을 찾을 수 없습니다.
)

echo.
echo 🚀 실행하려면: dotnet run
echo 또는 직접 실행: bin/Debug/net9.0/DummyClientCS.exe
echo.
pause 