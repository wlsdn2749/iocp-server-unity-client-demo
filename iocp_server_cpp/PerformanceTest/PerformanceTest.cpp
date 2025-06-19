// PerformanceTest.cpp : 애플리케이션의 진입점을 정의합니다.
//

#include <gtest/gtest.h>
#include <iostream>
#include <vector>
#include <string>
#include <thread>
#include <chrono>
#include <atomic>
#include <future>
#include <random>
#include <fstream>
#include <iomanip>

// 성능 테스트 결과 구조체
struct PerformanceResult {
	double tps = 0.0;                    // Transactions Per Second
	double averageLatency = 0.0;         // 평균 지연시간 (ms)
	double maxLatency = 0.0;             // 최대 지연시간 (ms)
	int successCount = 0;                // 성공한 요청 수
	int totalRequests = 0;               // 총 요청 수
	double successRate = 0.0;            // 성공률 (%)
	std::chrono::milliseconds duration{0}; // 테스트 소요 시간
};

// 클라이언트 시뮬레이터 클래스
class ClientSimulator {
public:
	ClientSimulator(int clientId) : _clientId(clientId) {}

	// 단일 패킷 전송 시뮬레이션 (네트워크 지연 포함)
	bool SendPacket(const std::string& packetType, int delayMs = 10) {
		auto start = std::chrono::high_resolution_clock::now();
		
		// 네트워크 지연 시뮬레이션
		std::this_thread::sleep_for(std::chrono::milliseconds(delayMs));
		
		auto end = std::chrono::high_resolution_clock::now();
		auto latency = std::chrono::duration_cast<std::chrono::milliseconds>(end - start);
		
		// 90% 성공률 시뮬레이션
		bool success = (rand() % 100) < 90;
		
		if (success) {
			_totalPacketsSent++;
			
			// atomic double에 대한 안전한 덧셈
			double latencyValue = static_cast<double>(latency.count());
			double currentTotal = _totalLatency.load();
			while (!_totalLatency.compare_exchange_weak(currentTotal, currentTotal + latencyValue)) {
				// compare_exchange_weak가 실패하면 currentTotal이 업데이트되므로 다시 시도
			}
			
			// atomic 변수에 대한 안전한 최대값 업데이트
			double currentMax = _maxLatency.load();
			while (latencyValue > currentMax && 
				   !_maxLatency.compare_exchange_weak(currentMax, latencyValue)) {
				// compare_exchange_weak가 실패하면 currentMax가 업데이트되므로 다시 시도
			}
		}
		
		return success;
	}

	// 게임 시나리오 실행: 로그인 → 이동 100회 → 채팅 10회 → 로그아웃
	PerformanceResult RunGameScenario() {
		auto startTime = std::chrono::high_resolution_clock::now();
		
		int successCount = 0;
		int totalRequests = 0;
		
		// 1. 로그인
		totalRequests++;
		if (SendPacket("LOGIN", 5)) successCount++;
		
		// 2. 이동 100회
		for (int i = 0; i < 100; i++) {
			totalRequests++;
			if (SendPacket("MOVE", 1)) successCount++;
		}
		
		// 3. 채팅 10회
		for (int i = 0; i < 10; i++) {
			totalRequests++;
			if (SendPacket("CHAT", 3)) successCount++;
		}
		
		// 4. 로그아웃
		totalRequests++;
		if (SendPacket("LOGOUT", 5)) successCount++;
		
		auto endTime = std::chrono::high_resolution_clock::now();
		auto duration = std::chrono::duration_cast<std::chrono::milliseconds>(endTime - startTime);
		
		PerformanceResult result;
		result.successCount = successCount;
		result.totalRequests = totalRequests;
		result.successRate = (static_cast<double>(successCount) / totalRequests) * 100.0;
		result.duration = duration;
		result.tps = (static_cast<double>(successCount) / duration.count()) * 1000.0; // TPS 계산
		result.averageLatency = _totalPacketsSent > 0 ? _totalLatency / _totalPacketsSent : 0.0;
		result.maxLatency = _maxLatency;
		
		return result;
	}

	int GetPacketsSent() const { return _totalPacketsSent; }

private:
	int _clientId;
	std::atomic<int> _totalPacketsSent{0};
	std::atomic<double> _totalLatency{0.0};
	std::atomic<double> _maxLatency{0.0};
};

// 성능 테스트 매니저
class PerformanceTestManager {
public:
	// 동시 사용자 테스트
	PerformanceResult RunConcurrentTest(int userCount, int testDurationSeconds = 10) {
		std::vector<std::thread> threads;
		std::vector<std::future<PerformanceResult>> futures;
		std::vector<std::promise<PerformanceResult>> promises(userCount);
		
		auto startTime = std::chrono::high_resolution_clock::now();
		
		// 각 사용자별 스레드 시작
		for (int i = 0; i < userCount; i++) {
			futures.push_back(promises[i].get_future());
			
			threads.emplace_back([this, i, testDurationSeconds, &promises]() {
				ClientSimulator client(i);
				auto result = client.RunGameScenario();
				promises[i].set_value(result);
			});
		}
		
		// 모든 스레드 완료 대기
		for (auto& thread : threads) {
			thread.join();
		}
		
		// 결과 집계
		PerformanceResult aggregatedResult;
		double totalTps = 0.0;
		double totalLatency = 0.0;
		int totalSuccess = 0;
		int totalRequests = 0;
		
		for (auto& future : futures) {
			auto result = future.get();
			totalTps += result.tps;
			totalLatency += result.averageLatency;
			totalSuccess += result.successCount;
			totalRequests += result.totalRequests;
			aggregatedResult.maxLatency = std::max(aggregatedResult.maxLatency, result.maxLatency);
		}
		
		auto endTime = std::chrono::high_resolution_clock::now();
		aggregatedResult.duration = std::chrono::duration_cast<std::chrono::milliseconds>(endTime - startTime);
		aggregatedResult.tps = totalTps;
		aggregatedResult.averageLatency = totalLatency / userCount;
		aggregatedResult.successCount = totalSuccess;
		aggregatedResult.totalRequests = totalRequests;
		aggregatedResult.successRate = (static_cast<double>(totalSuccess) / totalRequests) * 100.0;
		
		return aggregatedResult;
	}
};

// Google Test 케이스들
class PerformanceTestSuite : public ::testing::Test {
protected:
	void SetUp() override {
		manager = std::make_unique<PerformanceTestManager>();
		srand(static_cast<unsigned>(time(nullptr)));
	}
	
	void TearDown() override {
		manager.reset();
	}
	
	std::unique_ptr<PerformanceTestManager> manager;
};

// 기본 성능 테스트
TEST_F(PerformanceTestSuite, BasicPerformanceTest) {
	std::cout << "\n🚀 기본 성능 테스트 시작...\n";
	
	auto result = manager->RunConcurrentTest(1, 5);
	
	std::cout << "📊 결과:\n";
	std::cout << "   TPS: " << std::fixed << std::setprecision(2) << result.tps << "\n";
	std::cout << "   평균 지연시간: " << result.averageLatency << "ms\n";
	std::cout << "   성공률: " << result.successRate << "%\n";
	std::cout << "   총 요청: " << result.totalRequests << "\n";
	std::cout << "   성공: " << result.successCount << "\n";
	
	// 기본 성능 기준 검증
	EXPECT_GT(result.tps, 1.0) << "TPS가 너무 낮습니다: " << result.tps;
	EXPECT_GT(result.successRate, 80.0) << "성공률이 너무 낮습니다: " << result.successRate << "%";
	EXPECT_LT(result.averageLatency, 50.0) << "평균 지연시간이 너무 높습니다: " << result.averageLatency << "ms";
}

// 시나리오 기반 테스트
TEST_F(PerformanceTestSuite, GameScenarioTest) {
	std::cout << "\n🎮 게임 시나리오 테스트 시작...\n";
	std::cout << "   시나리오: 로그인 → 이동 100회 → 채팅 10회 → 로그아웃\n";
	
	ClientSimulator client(1);
	auto result = client.RunGameScenario();
	
	std::cout << "📊 시나리오 결과:\n";
	std::cout << "   총 패킷: " << client.GetPacketsSent() << "\n";
	std::cout << "   예상 패킷: 111개 (1+100+10+1)\n";
	std::cout << "   성공률: " << result.successRate << "%\n";
	std::cout << "   소요시간: " << result.duration.count() << "ms\n";
	
	// 시나리오 검증
	EXPECT_EQ(result.totalRequests, 112) << "총 요청 수가 예상과 다릅니다"; // 로그인(1) + 이동(100) + 채팅(10) + 로그아웃(1)
	EXPECT_GT(result.successRate, 70.0) << "시나리오 성공률이 너무 낮습니다";
	EXPECT_LT(result.duration.count(), 5000) << "시나리오 실행 시간이 너무 깁니다";
}

// 파라미터화된 동시 사용자 테스트
class ConcurrentUserTest : public PerformanceTestSuite, 
						  public ::testing::WithParamInterface<int> {
};

TEST_P(ConcurrentUserTest, MultipleUsers) {
	int userCount = GetParam();
	std::cout << "\n👥 동시 사용자 테스트: " << userCount << "명\n";
	
	auto result = manager->RunConcurrentTest(userCount, 3);
	
	std::cout << "📊 " << userCount << "명 동시 접속 결과:\n";
	std::cout << "   총 TPS: " << std::fixed << std::setprecision(2) << result.tps << "\n";
	std::cout << "   사용자당 TPS: " << (result.tps / userCount) << "\n";
	std::cout << "   평균 지연시간: " << result.averageLatency << "ms\n";
	std::cout << "   최대 지연시간: " << result.maxLatency << "ms\n";
	std::cout << "   성공률: " << result.successRate << "%\n";
	std::cout << "   테스트 시간: " << result.duration.count() << "ms\n";
	
	// 동시 사용자 성능 기준
	double expectedMinTpsPerUser = 2.0; // 사용자당 최소 2 TPS
	double actualTpsPerUser = result.tps / userCount;
	
	EXPECT_GT(actualTpsPerUser, expectedMinTpsPerUser) 
		<< "사용자당 TPS가 기준치보다 낮습니다. 기대: " << expectedMinTpsPerUser 
		<< ", 실제: " << actualTpsPerUser;
	
	EXPECT_GT(result.successRate, 75.0) 
		<< userCount << "명 동시 접속 시 성공률이 너무 낮습니다: " << result.successRate << "%";
	
	EXPECT_LT(result.averageLatency, 100.0) 
		<< userCount << "명 동시 접속 시 평균 지연시간이 너무 높습니다: " << result.averageLatency << "ms";
}

// 다양한 동시 사용자 수로 테스트
INSTANTIATE_TEST_SUITE_P(
	VariousUserCounts,
	ConcurrentUserTest,
	::testing::Values(1, 3, 5, 8, 10)
);

// 성능 회귀 테스트 (CSV 출력)
TEST_F(PerformanceTestSuite, PerformanceRegressionTest) {
	std::cout << "\n📈 성능 회귀 테스트 및 결과 저장...\n";
	
	std::vector<int> userCounts = {1, 5, 10};
	std::ofstream csvFile("performance_results.csv");
	
	csvFile << "UserCount,TPS,AvgLatency,MaxLatency,SuccessRate,Duration\n";
	
	for (int users : userCounts) {
		auto result = manager->RunConcurrentTest(users, 3);
		
		csvFile << users << ","
				<< std::fixed << std::setprecision(2) << result.tps << ","
				<< result.averageLatency << ","
				<< result.maxLatency << ","
				<< result.successRate << ","
				<< result.duration.count() << "\n";
		
		std::cout << "   " << users << "명: TPS=" << result.tps 
				  << ", 지연=" << result.averageLatency << "ms\n";
	}
	
	csvFile.close();
	std::cout << "📄 결과가 performance_results.csv에 저장되었습니다.\n";
	
	SUCCEED() << "성능 회귀 테스트 완료";
}
