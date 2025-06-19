@echo off
chcp 65001
echo.
echo 🚀 실제 네트워크 성능 테스트 시작
echo ===============================================

cd /d "%~dp0"

echo.
echo 📦 1단계: GameServer 빌드 중...
echo ===============================================
cd ..
echo [DEBUG] 현재 경로: %CD%
if exist Binary/Debug/GameServer.exe (
    echo ✅ Debug 버전 GameServer.exe 발견
) else if exist Binary/Release/GameServer.exe (
    echo ✅ Release 버전 GameServer.exe 발견
) else (
    echo ❌ GameServer.exe를 찾을 수 없습니다.
    echo    Visual Studio에서 GameServer 프로젝트를 빌드하세요.
    echo    또는 다음 명령어를 실행하세요:
    echo    msbuild GameServer.vcxproj /p:Configuration=Debug /p:Platform=x64
    pause
    exit /b 1
)

echo.
echo 📦 2단계: DummyClientCS 빌드 중...
echo ===============================================
cd DummyClientCS
echo [DEBUG] 현재 경로: %CD%
if exist bin/Debug/net9.0/DummyClientCS.exe (
    echo ✅ Debug 버전 DummyClientCS.exe 발견
) else if exist bin/Release/net9.0/DummyClientCS.exe (
    echo ✅ Release 버전 DummyClientCS.exe 발견
) else (
    echo 🔨 DummyClientCS만 빌드 시도 중...
    REM 특정 프로젝트 파일만 빌드 (솔루션 전체 빌드 방지)
    dotnet build DummyClientCS.csproj
    if errorlevel 1 (
        echo ❌ DummyClientCS 빌드 실패
        echo    .NET 9.0 SDK가 설치되어 있는지 확인하세요.
        pause
        exit /b 1
    )
    echo ✅ DummyClientCS 빌드 완료
)

echo.
echo 🧪 3단계: 성능 테스트 빌드 및 실행 준비...
echo ===============================================
cd ../PerformanceTest
echo [DEBUG] 현재 경로: %CD%

:: Google Test 실행 파일 확인
if exist build\Debug\PerformanceTest.exe (
    set TEST_EXE=build\Debug\PerformanceTest.exe
    echo ✅ Debug 버전 테스트 실행 파일 발견
) else if exist build\Release\PerformanceTest.exe (
    set TEST_EXE=build\Release\PerformanceTest.exe
    echo ✅ Release 버전 테스트 실행 파일 발견
) else (
    echo 🔨 PerformanceTest.exe를 찾을 수 없습니다. 자동 빌드 시도...
    
    :: build 디렉토리 생성
    if not exist build mkdir build
    cd build
    
    :: CMake 구성
    echo 📦 CMake 구성 중...
    cmake ..
    if errorlevel 1 (
        echo ❌ CMake 구성 실패
        pause
        exit /b 1
    )
    
    :: Release 빌드
    echo 📦 Release 빌드 중...
    cmake --build . --config Release
    if errorlevel 1 (
        echo ❌ 빌드 실패
        pause
        exit /b 1
    )
    
    :: 빌드 완료 후 실행 파일 확인
    cd ..
    if exist build\Release\PerformanceTest.exe (
        set TEST_EXE=build\Release\PerformanceTest.exe
        echo ✅ PerformanceTest 자동 빌드 완료!
    ) else (
        echo ❌ 빌드 후에도 실행 파일을 찾을 수 없습니다.
        pause
        exit /b 1
    )
)

echo.
echo 🎯 4단계: 실제 네트워크 성능 테스트 실행...
echo ===============================================
echo 주의: 이 테스트는 실제 서버와 클라이언트 프로세스를 시작합니다.
echo       테스트가 완료될 때까지 기다려주세요.
echo.
echo 📋 실행 중인 테스트: RealNetworkTestSuite (실제 네트워크)
echo    - 더미 테스트가 아닌 실제 GameServer + DummyClientCS 통신 테스트
echo    - 필터: --gtest_filter=RealNetworkTestSuite.*
echo.

:: 실제 네트워크 테스트만 실행 (RealNetworkTestSuite 필터)
%TEST_EXE% --gtest_filter=RealNetworkTestSuite.* --gtest_output=xml:real_network_test_results.xml

echo.
echo 📊 5단계: 테스트 결과 확인...
echo ===============================================

if exist real_network_performance_report.csv (
    echo ✅ 성능 리포트 생성됨: real_network_performance_report.csv
    echo.
    echo 📄 CSV 리포트 내용:
    type real_network_performance_report.csv
) else (
    echo ⚠️  성능 리포트가 생성되지 않았습니다.
)

if exist real_network_test_results.xml (
    echo ✅ XML 테스트 결과 생성됨: real_network_test_results.xml
) else (
    echo ⚠️  XML 테스트 결과가 생성되지 않았습니다.
)

echo.
echo 🏁 실제 네트워크 성능 테스트 완료!
echo ===============================================
echo.
echo 다음 파일들을 확인하세요:
echo - real_network_performance_report.csv (성능 데이터)
echo - real_network_test_results.xml (상세 테스트 결과)
echo.
pause 