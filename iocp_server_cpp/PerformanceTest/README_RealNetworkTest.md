# 🌐 실제 네트워크 성능 테스트 가이드

이 가이드는 **실제 GameServer와 DummyClientCS 간의 네트워크 통신**을 테스트하는 방법을 설명합니다.

## 📋 테스트 개요

**중요**: 하나의 `PerformanceTest.exe` 실행파일에 두 종류의 테스트가 포함되어 있습니다.

### 🤖 PerformanceTestSuite (가상/더미 테스트)
- **코드**: `PerformanceTest.cpp`
- **목적**: 네트워크 시뮬레이션을 통한 성능 벤치마크
- **특징**: `sleep()` 함수로 네트워크 지연 시뮬레이션
- **실행**: `PerformanceTest.exe --gtest_filter=PerformanceTestSuite.*`
- **장점**: 빠른 실행, 외부 종속성 없음
- **단점**: 실제 네트워크 상황과 다를 수 있음

### 🌐 RealNetworkTestSuite (실제 네트워크 테스트) ⭐
- **코드**: `RealNetworkPerformanceTest.cpp`
- **목적**: 실제 서버-클라이언트 간 네트워크 통신 테스트
- **특징**: GameServer.exe + DummyClientCS.exe 프로세스 실행
- **실행**: `PerformanceTest.exe --gtest_filter=RealNetworkTestSuite.*`
- **장점**: 실제 네트워크 환경 테스트
- **단점**: 빌드 파일 필요, 실행 시간 longer

## 🚀 빠른 시작

### ⭐ **핵심 포인트**
- 실행파일은 `PerformanceTest.exe` **하나**입니다
- **실제 네트워크 테스트**를 원한다면: `--gtest_filter=RealNetworkTestSuite.*` 사용
- **더미 테스트**를 원한다면: `--gtest_filter=PerformanceTestSuite.*` 사용

### 1단계: 자동 빌드 및 테스트 실행
```bash
# 실제 네트워크 테스트 자동 실행 (추천!)
cd iocp_server_cpp/PerformanceTest
build_and_test_real_network.bat
```

### 2단계: 수동 빌드 (필요시)

#### GameServer 빌드
```bash
# Visual Studio 사용
cd iocp_server_cpp/GameServer
# GameServer.vcxproj 열기 후 빌드 (Ctrl+Shift+B)

# 또는 MSBuild 사용
msbuild GameServer.vcxproj /p:Configuration=Debug /p:Platform=x64
```

#### DummyClientCS 빌드
```bash
cd iocp_server_cpp/DummyClientCS
dotnet build DummyClientCS.csproj
```

#### PerformanceTest 빌드
```bash
cd iocp_server_cpp/PerformanceTest
mkdir build && cd build
cmake ..
cmake --build . --config Release
```

## 🧪 테스트 종류

### 🎯 서버 시작 테스트
- **목적**: GameServer.exe가 정상적으로 시작되는지 확인
- **테스트**: `ServerStartupTest`
- **소요시간**: ~5초

### 🔗 단일 클라이언트 연결 테스트  
- **목적**: 1개 클라이언트의 서버 연결 확인
- **테스트**: `SingleClientConnectionTest`
- **소요시간**: ~10초

### 💪 다중 클라이언트 스트레스 테스트
- **목적**: 5개 클라이언트 동시 연결 테스트
- **테스트**: `MultiClientStressTest`
- **소요시간**: ~15초

### ⏱️ 내구성 테스트 (선택적)
- **목적**: 30초간 장시간 연결 유지 테스트
- **테스트**: `EnduranceTest` (기본적으로 비활성화)
- **활성화**: `--gtest_filter=*EnduranceTest*`

### 📈 성능 리포트 생성
- **목적**: CSV 파일로 성능 데이터 저장
- **테스트**: `PerformanceReportTest`
- **출력**: `real_network_performance_report.csv`

## 📊 결과 분석

### CSV 리포트 예시
```csv
ClientCount,TPS,AvgLatency,MaxLatency,PacketLoss,Duration
1,4.00,10.50,21.00,2.00,5023
3,12.00,11.50,23.00,2.00,5034
5,20.00,12.50,25.00,2.00,5045
```

### 성능 지표 설명
- **TPS**: Transactions Per Second (초당 처리 트랜잭션 수)
- **AvgLatency**: 평균 응답 지연시간 (ms)
- **MaxLatency**: 최대 응답 지연시간 (ms) 
- **PacketLoss**: 패킷 손실률 (%)
- **Duration**: 테스트 소요 시간 (ms)

## 🛠️ 고급 사용법

### 특정 테스트만 실행
```bash
# 서버 시작 테스트만 실행
PerformanceTest.exe --gtest_filter=RealNetworkTestSuite.ServerStartupTest

# 스트레스 테스트만 실행  
PerformanceTest.exe --gtest_filter=*StressTest*

# 실제 네트워크 관련 모든 테스트 실행
PerformanceTest.exe --gtest_filter=RealNetworkTestSuite.*
```

### XML 결과 출력
```bash
PerformanceTest.exe --gtest_output=xml:test_results.xml
```

### 상세 로그 출력
```bash
PerformanceTest.exe --gtest_filter=RealNetworkTestSuite.* -v
```

## ⚠️ 주의사항

### 필수 요구사항
1. ✅ **GameServer.exe** 빌드 완료
2. ✅ **DummyClientCS.exe** 빌드 완료  
3. ✅ **PerformanceTest.exe** 빌드 완료
4. ✅ 포트 **8421** 사용 가능

### 네트워크 환경
- 로컬호스트 (`127.0.0.1`) 사용
- 포트 `8421`에서 서버 실행
- 방화벽 설정 확인 필요

### 프로세스 관리
- 테스트 완료 후 자동으로 프로세스 정리
- 수동 종료가 필요한 경우 작업 관리자 사용
- 포트가 이미 사용 중인 경우 재시작 필요

## 🐛 문제 해결

### GameServer.exe를 찾을 수 없음
```
❌ GameServer 실행 파일을 찾을 수 없습니다.
```
**해결방법**: Visual Studio에서 GameServer 프로젝트 빌드

### DummyClientCS.exe를 찾을 수 없음  
```
❌ DummyClientCS 실행 파일을 찾을 수 없습니다.
```
**해결방법**: `.NET 9.0 SDK` 설치 후 `dotnet build DummyClientCS.csproj` 실행

### 포트 사용 중 오류
```
❌ 포트 8421이 이미 사용 중입니다.
```
**해결방법**: 
```bash
netstat -ano | findstr :8421
taskkill /PID [PID번호] /F
```

### 프로세스 정리 안됨
**해결방법**: 작업 관리자에서 `GameServer.exe`, `DummyClientCS.exe` 수동 종료

## 🎯 성능 벤치마크 목표

### 기본 성능 기준
- **단일 클라이언트**: TPS ≥ 4.0
- **다중 클라이언트**: 총 TPS ≥ 10.0
- **평균 지연시간**: ≤ 100ms
- **패킷 손실률**: ≤ 5%

### 최적화 지표
- **클라이언트당 TPS**: ≥ 2.0
- **동시 연결 수**: ≥ 10개
- **최대 지연시간**: ≤ 200ms
- **메모리 사용량**: 모니터링 필요

---

## 📞 추가 지원

문제가 발생하거나 추가 기능이 필요한 경우:
1. 로그 파일 확인: `real_network_test_results.xml`
2. 성능 데이터 분석: `real_network_performance_report.csv`
3. 프로세스 상태 확인: 작업 관리자 