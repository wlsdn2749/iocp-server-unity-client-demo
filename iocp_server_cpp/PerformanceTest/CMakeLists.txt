# CMakeList.txt: PerformanceTest에 대한 CMake 프로젝트
cmake_minimum_required(VERSION 3.14)

project(PerformanceTest)

# C++ 17 표준 사용
set(CMAKE_CXX_STANDARD 17)
set(CMAKE_CXX_STANDARD_REQUIRED ON)

# 빌드 출력 디렉토리 설정
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_DEBUG ${CMAKE_CURRENT_SOURCE_DIR}/build/Debug)
set(CMAKE_RUNTIME_OUTPUT_DIRECTORY_RELEASE ${CMAKE_CURRENT_SOURCE_DIR}/build/Release)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_DEBUG ${CMAKE_CURRENT_SOURCE_DIR}/build/Debug)
set(CMAKE_LIBRARY_OUTPUT_DIRECTORY_RELEASE ${CMAKE_CURRENT_SOURCE_DIR}/build/Release)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_DEBUG ${CMAKE_CURRENT_SOURCE_DIR}/build/Debug)
set(CMAKE_ARCHIVE_OUTPUT_DIRECTORY_RELEASE ${CMAKE_CURRENT_SOURCE_DIR}/build/Release)

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

# 실제 네트워크 성능 테스트 실행 파일 생성
add_executable(
  NetworkPerformanceTest
  RealNetworkPerformanceTest.cpp
)

# Google Test 라이브러리 링크
target_link_libraries(
  NetworkPerformanceTest
  gtest_main
  gtest
)

# 테스트 발견 활성화
include(GoogleTest)
gtest_discover_tests(NetworkPerformanceTest)
