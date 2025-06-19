# 🚀 IOCP 서버 성능 테스트

## 📋 개요
이 프로젝트는 IOCP 서버의 성능을 측정하고 벤치마크하는 Google Test 기반 테스트 스위트입니다.

## 🎯 테스트 시나리오
**게임 클라이언트 시뮬레이션**: 로그인 → 이동 100회 → 채팅 10회 → 로그아웃

## 🧪 테스트 종류

### 1. 기본 성능 테스트 (`BasicPerformanceTest`)
- 단일 사용자 기본 성능 측정
- TPS, 지연시간, 성공률 검증

### 2. 게임 시나리오 테스트 (`GameScenarioTest`)
- 실제 게임 플레이 패턴 시뮬레이션
- 패킷 수, 실행 시간 검증

### 3. 동시 사용자 테스트 (`ConcurrentUserTest`)
- 1, 3, 5, 8, 10명 동시 접속 테스트
- 사용자당 성능 기준 검증

### 4. 성능 회귀 테스트 (`PerformanceRegressionTest`)
- 결과를 CSV 파일로 저장
- 성능 트렌드 분석 가능

## 🚀 실행 방법

### 방법 1: 배치 파일 사용 (권장)
```bash
# PerformanceTest 디렉토리에서
./run_tests.bat
```

### 방법 2: 수동 실행
```bash
# 1. 빌드 디렉토리 생성
mkdir build
cd build

# 2. CMake 구성
cmake .. -G "Visual Studio 17 2022" -A x64

# 3. 빌드
cmake --build . --config Release

# 4. 테스트 실행
Release/PerformanceTest.exe
```

### 방법 3: Visual Studio에서 실행
1. Visual Studio에서 `iocp_server_cpp` 폴더 열기
2. CMake 프로젝트로 인식됨
3. `PerformanceTest` 타겟 선택
4. F5 또는 Ctrl+F5로 실행

## 📊 성능 기준

| 테스트 | 기준 | 설명 |
|--------|------|------|
| TPS | > 1.0 | 초당 트랜잭션 수 |
| 성공률 | > 80% | 패킷 전송 성공률 |
| 평균 지연시간 | < 50ms | 응답 시간 |
| 사용자당 TPS | > 2.0 | 동시 접속 시 사용자당 성능 |

## 📈 결과 분석

### 콘솔 출력
```
🚀 기본 성능 테스트 시작...
📊 결과:
   TPS: 25.64
   평균 지연시간: 3.50ms
   성공률: 89.29%
   총 요청: 112
   성공: 100
```

### CSV 파일 (`performance_results.csv`)
```csv
UserCount,TPS,AvgLatency,MaxLatency,SuccessRate,Duration
1,25.64,3.50,5.00,89.29,4375
5,128.20,3.80,7.00,87.50,21875
10,256.40,4.20,9.00,85.71,43750
```

## 🔧 커스터마이징

### 테스트 파라미터 수정
```cpp
// 동시 사용자 수 변경
INSTANTIATE_TEST_SUITE_P(
    VariousUserCounts,
    ConcurrentUserTest,
    ::testing::Values(1, 3, 5, 8, 10, 15, 20) // 여기에 원하는 수치 추가
);

// 성능 기준 변경
EXPECT_GT(result.tps, 2.0) // TPS 기준 변경
EXPECT_GT(result.successRate, 85.0) // 성공률 기준 변경
```

### 새로운 테스트 시나리오 추가
```cpp
TEST_F(PerformanceTestSuite, CustomScenarioTest) {
    // 사용자 정의 시나리오 구현
}
```

## 🛠️ 요구사항
- **CMake**: 3.14 이상
- **Visual Studio**: 2022 (v143 toolset)
- **C++ 표준**: C++17
- **Google Test**: 자동 다운로드됨

## 📝 주의사항
1. 첫 실행 시 Google Test 다운로드로 인해 시간이 소요될 수 있습니다.
2. 방화벽이나 안티바이러스에서 차단될 수 있습니다.
3. 성능 측정 시 다른 프로그램을 최소화하여 정확한 결과를 얻으세요.

## 🚀 CI/CD 연동
이 테스트는 GitHub Actions, Jenkins 등 CI/CD 파이프라인에서도 실행 가능합니다.

```yaml
# GitHub Actions 예시
- name: Run Performance Tests
  run: |
    cd iocp_server_cpp/PerformanceTest
    ./run_tests.bat
``` 