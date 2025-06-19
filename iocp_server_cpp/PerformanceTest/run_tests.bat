@echo off
echo ==============================================
echo 🚀 IOCP 서버 성능 테스트 시작
echo ==============================================

cd /d "%~dp0"

echo.
echo 📁 현재 디렉토리: %cd%
echo.

echo 🔨 CMake 빌드 시작...
if not exist "build" mkdir build
cd build

echo.
echo ⚙️ CMake 구성 중...
cmake .. -G "Visual Studio 17 2022" -A x64

if %ERRORLEVEL% neq 0 (
    echo ❌ CMake 구성 실패!
    pause
    exit /b 1
)

echo.
echo 🔧 프로젝트 빌드 중...
cmake --build . --config Release

if %ERRORLEVEL% neq 0 (
    echo ❌ 빌드 실패!
    pause
    exit /b 1
)

echo.
echo ✅ 빌드 완료!
echo.
echo 🧪 테스트 실행 중...
echo ==============================================

Release\PerformanceTest.exe

echo.
echo ==============================================
echo 📊 테스트 완료!
echo.
echo 📄 결과 파일 확인:
if exist "performance_results.csv" (
    echo    ✅ performance_results.csv 생성됨
    echo.
    echo 📈 CSV 파일 내용:
    type performance_results.csv
) else (
    echo    ⚠️  performance_results.csv 파일이 생성되지 않았습니다.
)

echo.
echo ==============================================
pause 