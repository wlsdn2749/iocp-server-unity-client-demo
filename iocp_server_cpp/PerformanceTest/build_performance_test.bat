@echo off
chcp 65001
echo.
echo 🔨 PerformanceTest 빌드
echo ===============================================

cd /d "%~dp0"

echo 📦 1단계: build 디렉토리 생성/확인...
if not exist build mkdir build

echo 📦 2단계: CMake 구성...
cd build
cmake ..
if errorlevel 1 (
    echo ❌ CMake 구성 실패
    pause
    exit /b 1
)

echo 📦 3단계: Release 빌드...
cmake --build . --config Release
if errorlevel 1 (
    echo ❌ 빌드 실패
    pause
    exit /b 1
)

echo.
echo ✅ PerformanceTest 빌드 완료!
echo ===============================================

if exist Release\PerformanceTest.exe (
    echo 📄 빌드된 파일: build\Release\PerformanceTest.exe
    echo.
    echo 🚀 테스트 실행:
    echo    Release\PerformanceTest.exe
    echo.
    echo 🌐 실제 네트워크 테스트 실행:
    echo    Release\PerformanceTest.exe --gtest_filter=RealNetworkTestSuite.*
) else (
    echo ⚠️  실행 파일을 찾을 수 없습니다.
)

echo.
pause 