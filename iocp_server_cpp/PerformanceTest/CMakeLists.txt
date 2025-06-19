# CMakeList.txt: PerformanceTest에 대한 CMake 프로젝트
cmake_minimum_required(VERSION 3.14)

project(PerformanceTest)

# C++ 17 표준 사용
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# Google Test 다운로드 및 빌드 (안정적인 릴리스 버전 사용)
include(FetchContent)
FetchContent_Declare(
  googletest
  URL https://github.com/google/googletest/releases/download/v1.17.0/googletest-1.17.0.tar.gz
  DOWNLOAD_EXTRACT_TIMESTAMP TRUE
)

# Windows에서 gtest_force_shared_crt 설정
set(gtest_force_shared_crt ON CACHE BOOL "" FORCE)
FetchContent_MakeAvailable(googletest)

# 테스트 실행 파일 생성
add_executable(
  PerformanceTest
  PerformanceTest.cpp
  RealNetworkPerformanceTest.cpp
)

# Google Test 라이브러리 링크
target_link_libraries(
  PerformanceTest
  gtest_main
  gtest
)

# 테스트 발견 활성화
include(GoogleTest)
gtest_discover_tests(PerformanceTest)
