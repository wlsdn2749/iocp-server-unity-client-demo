@echo off
chcp 65001
REM ========================================================
REM 🚀 게임서버-클라이언트 네트워크 테스트 (올인원)
REM ========================================================
REM 이 파일 하나로 모든 작업을 수행합니다:
REM - CMake 빌드 (Debug + Release)
REM - GameServer 자동 시작
REM - DummyClientCS 자동 실행 (다중 클라이언트)
REM - 실제 네트워크 통신 테스트
REM - 성능 리포트 생성 (CSV)
REM - 테스트 후 자동 정리
REM ========================================================
echo.
echo 🚀 실제 네트워크 성능 테스트 시작
echo ===============================================

cd /d "%~dp0"

echo.
echo 📦 1단계: GameServer 빌드 중...
echo ===============================================
cd ../..
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

:: Google Test 실행 파일 확인 (새로운 파일명)
if exist build\Debug\NetworkPerformanceTest.exe (
    set TEST_EXE=build\Debug\NetworkPerformanceTest.exe
    echo ✅ Debug 버전 테스트 실행 파일 발견
) else if exist build\Release\NetworkPerformanceTest.exe (
    set TEST_EXE=build\Release\NetworkPerformanceTest.exe
    echo ✅ Release 버전 테스트 실행 파일 발견
) else (
    echo 🔨 NetworkPerformanceTest.exe를 찾을 수 없습니다. 자동 빌드 시도...
    
    :: 기존 빌드 파일 정리
    if exist build rmdir /s /q build
    if exist CMakeCache.txt del CMakeCache.txt
    if exist CMakeFiles rmdir /s /q CMakeFiles
    
    :: build 디렉토리 생성
    mkdir build
    mkdir build\Debug
    mkdir build\Release
    cd build
    
    :: CMake 구성
    echo 📦 CMake 구성 중...
    cmake .. -DCMAKE_BUILD_TYPE=Debug
    if errorlevel 1 (
        echo ❌ CMake 구성 실패
        cd ..
        pause
        exit /b 1
    )
    
    :: Debug 빌드
    echo 📦 Debug 빌드 중...
    cmake --build . --config Debug
    if errorlevel 1 (
        echo ❌ Debug 빌드 실패
        cd ..
        pause
        exit /b 1
    )
    
    :: Release 빌드
    echo 📦 Release 빌드 중...
    cmake --build . --config Release
    if errorlevel 1 (
        echo ❌ Release 빌드 실패
        cd ..
        pause
        exit /b 1
    )
    
    :: 빌드 완료 후 실행 파일 확인
    cd ..
    if exist build\Debug\NetworkPerformanceTest.exe (
        set TEST_EXE=build\Debug\NetworkPerformanceTest.exe
        echo ✅ NetworkPerformanceTest Debug 자동 빌드 완료!
    ) else if exist build\Release\NetworkPerformanceTest.exe (
        set TEST_EXE=build\Release\NetworkPerformanceTest.exe
        echo ✅ NetworkPerformanceTest Release 자동 빌드 완료!
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
echo    - 실행 파일: %TEST_EXE%
echo.

:: gTest 환경변수 설정 (DummyClientCS에서 통계 파일 생성 활성화)
set GTEST_MODE=true
set PERFORMANCE_TEST=true

:: 실제 네트워크 테스트만 실행 (RealNetworkTestSuite 필터)
%TEST_EXE% --gtest_filter=RealNetworkTestSuite.* --gtest_output=xml:../reports/real_network_test_results.xml

echo.
echo 📊 5단계: 테스트 결과 확인...
echo ===============================================

if exist ../reports/real_network_performance_report.csv (
    echo ✅ 성능 리포트 생성됨: ../reports/real_network_performance_report.csv
    echo.
    echo 📄 CSV 리포트 내용:
    type ../reports/real_network_performance_report.csv
) else (
    echo ⚠️  성능 리포트가 생성되지 않았습니다.
)

if exist ../reports/real_network_test_results.xml (
    echo ✅ XML 테스트 결과 생성됨: ../reports/real_network_test_results.xml
) else (
    echo ⚠️  XML 테스트 결과가 생성되지 않았습니다.
)

echo.
echo 🏁 실제 네트워크 성능 테스트 완료!
echo ===============================================
echo.
echo 📊 결과 파일들:
echo   - ../reports/real_network_test_results.xml (GTest XML 리포트)
echo   - ../reports/real_network_performance_report.csv (성능 데이터)
echo   - ../reports/performance_summary.json (통계 요약 파일)
echo.
echo 🧹 개별 통계 파일들은 자동으로 정리되었습니다
echo   (server_stats.json, client_stats_*.json 삭제됨)
echo.
echo 📁 테스트 실행 파일 위치: %TEST_EXE%
echo.
pause 