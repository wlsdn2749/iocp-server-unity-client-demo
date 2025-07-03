#pragma once
#include <atomic>
#include <string>
#include <unordered_map>
#include <thread>
#include <chrono>
#include <sstream>
#include <winsock2.h>
#include <ws2tcpip.h>
#pragma comment(lib, "ws2_32.lib")
#include <iostream>
#include <cstdint>
#include <cmath>
#include <cstring>  

/*-----------------
  PrometheusMetrics
 -----------------*/

class PrometheusMetrics
{
private:
    // 기본 메트릭들
    std::atomic<int32> _connectedClients{0};
    std::atomic<int64> _totalPacketsReceived{0};
    std::atomic<int64> _totalPacketsSent{0};
    std::atomic<int64> _movePacketsReceived{0};
    std::atomic<int64> _chatPacketsReceived{0};
    std::atomic<int64> _rttPacketsReceived{0};
    
    // JobQueue 메트릭들
    std::atomic<int64> _jobQueuePushedTotal{0};
    std::atomic<int64> _jobQueueExecutedTotal{0};
    std::atomic<int64> _jobQueuePendingCount{0};
    
    // 성능 메트릭들
    std::atomic<double> _avgProcessingTime{0.0};
    std::atomic<double> _maxProcessingTime{0.0};
    std::atomic<int64> _memoryUsageBytes{0};
    std::atomic<int64> _poolMemoryBytes{0};
    std::atomic<int64> _processingTimeCount{0};
    std::atomic<double> _totalProcessingTime{0.0};
    

    
    // 히스토그램 버킷들 (패킷 처리 시간)
    std::atomic<int64> _processingTimeBuckets[10]{};  // 0.1ms, 0.5ms, 1ms, 5ms, 10ms, 50ms, 100ms, 500ms, 1000ms, +Inf
    
    // 최근 통계 (TPS 계산용)
    std::atomic<int64> _recentPacketsReceived{0};
    std::atomic<int64> _recentPacketsSent{0};
    std::chrono::steady_clock::time_point _lastTpsUpdate;
    std::atomic<double> _currentTps{0.0};
    
    // HTTP 서버 관련
    std::thread _httpServerThread;
    std::atomic<bool> _running{false};
    int32 _httpPort = 10001;

public:
    PrometheusMetrics() : _lastTpsUpdate(std::chrono::steady_clock::now()) {}
    ~PrometheusMetrics() { Stop(); }

    // 메트릭 업데이트 함수들
    void IncrementConnectedClients() { _connectedClients++; }
    void DecrementConnectedClients() { _connectedClients--; }
    void IncrementPacketsReceived() { _totalPacketsReceived++; _recentPacketsReceived++; }
    void IncrementPacketsSent() { _totalPacketsSent++; _recentPacketsSent++; }
    void IncrementMovePackets() { _movePacketsReceived++; }
    void IncrementChatPackets() { _chatPacketsReceived++; }
    void IncrementRttPackets() { _rttPacketsReceived++; }
    
    // JobQueue 메트릭 업데이트 함수들
    void IncrementJobQueuePushed() { _jobQueuePushedTotal++; }
    void IncrementJobQueueExecuted() { _jobQueueExecutedTotal++; }
    void UpdateJobQueuePending(int64 pendingCount) { _jobQueuePendingCount = pendingCount; }
    
    void UpdateProcessingTime(double timeMs) {
        // 최대값 업데이트
        double currentMax = _maxProcessingTime.load();
        while (timeMs > currentMax && 
               !_maxProcessingTime.compare_exchange_weak(currentMax, timeMs)) {
            // 재시도
        }
        
        // 평균값 계산을 위한 누적
        _processingTimeCount++;
        double currentTotal = _totalProcessingTime.load();
        while (!_totalProcessingTime.compare_exchange_weak(currentTotal, currentTotal + timeMs)) {
            // 재시도
        }
        
        // 평균값 업데이트 (매번 계산)
        int64 count = _processingTimeCount.load();
        if (count > 0) {
            _avgProcessingTime = _totalProcessingTime.load() / count;
        }
        
        // 히스토그램 버킷 업데이트
        const double buckets[] = {0.1, 0.5, 1.0, 5.0, 10.0, 50.0, 100.0, 500.0, 1000.0, INFINITY};
        for (int i = 0; i < 10; i++) {
            if (timeMs <= buckets[i]) {
                _processingTimeBuckets[i]++;
                break;
            }
        }
    }
    
    void UpdateMemoryUsage(int64 heapBytes, int64 poolBytes) {
        _memoryUsageBytes = heapBytes;
        _poolMemoryBytes = poolBytes;
    }

    // TPS 계산 (주기적 호출 필요)
    void UpdateTps() {
        auto now = std::chrono::steady_clock::now();
        auto elapsed = std::chrono::duration_cast<std::chrono::milliseconds>(now - _lastTpsUpdate);
        
        if (elapsed.count() >= 1000) {  // 1초마다 업데이트
            double elapsedSeconds = elapsed.count() / 1000.0;
            _currentTps = (_recentPacketsReceived.load() + _recentPacketsSent.load()) / elapsedSeconds;
            
            _recentPacketsReceived = 0;
            _recentPacketsSent = 0;
            _lastTpsUpdate = now;
        }
    }



    // Prometheus 포맷으로 메트릭 출력
    std::string GenerateMetrics() {
        std::ostringstream oss;
        
        // 게이지 메트릭들
        oss << "# HELP server_connected_clients Currently connected clients\n";
        oss << "# TYPE server_connected_clients gauge\n";
        oss << "server_connected_clients " << _connectedClients.load() << "\n\n";
        
        // 카운터 메트릭들
        oss << "# HELP server_packets_received_total Total packets received\n";
        oss << "# TYPE server_packets_received_total counter\n";
        oss << "server_packets_received_total{type=\"total\"} " << _totalPacketsReceived.load() << "\n";
        oss << "server_packets_received_total{type=\"move\"} " << _movePacketsReceived.load() << "\n";
        oss << "server_packets_received_total{type=\"chat\"} " << _chatPacketsReceived.load() << "\n";
        oss << "server_packets_received_total{type=\"rtt\"} " << _rttPacketsReceived.load() << "\n\n";
        
        oss << "# HELP server_packets_sent_total Total packets sent\n";
        oss << "# TYPE server_packets_sent_total counter\n";
        oss << "server_packets_sent_total " << _totalPacketsSent.load() << "\n\n";
        

        
        // TPS 메트릭
        oss << "# HELP server_tps_current Current transactions per second\n";
        oss << "# TYPE server_tps_current gauge\n";
        oss << "server_tps_current " << _currentTps.load() << "\n\n";
        
        // 성능 메트릭들
        oss << "# HELP server_processing_time_avg_ms Average processing time in milliseconds\n";
        oss << "# TYPE server_processing_time_avg_ms gauge\n";
        oss << "server_processing_time_avg_ms " << _avgProcessingTime.load() << "\n\n";
        
        oss << "# HELP server_processing_time_max_ms Maximum processing time in milliseconds\n";
        oss << "# TYPE server_processing_time_max_ms gauge\n";
        oss << "server_processing_time_max_ms " << _maxProcessingTime.load() << "\n\n";
        
        // 히스토그램 메트릭
        oss << "# HELP server_processing_time_seconds Processing time histogram\n";
        oss << "# TYPE server_processing_time_seconds histogram\n";
        const double buckets[] = {0.0001, 0.0005, 0.001, 0.005, 0.01, 0.05, 0.1, 0.5, 1.0};
        for (int i = 0; i < 9; i++) {
            oss << "server_processing_time_seconds_bucket{le=\"" << buckets[i] << "\"} " 
                << _processingTimeBuckets[i].load() << "\n";
        }
        oss << "server_processing_time_seconds_bucket{le=\"+Inf\"} " 
            << _processingTimeBuckets[9].load() << "\n\n";
        
        // JobQueue 메트릭들
        oss << "# HELP server_jobqueue_pushed_total Total jobs pushed to JobQueue\n";
        oss << "# TYPE server_jobqueue_pushed_total counter\n";
        oss << "server_jobqueue_pushed_total " << _jobQueuePushedTotal.load() << "\n\n";
        
        oss << "# HELP server_jobqueue_executed_total Total jobs executed from JobQueue\n";
        oss << "# TYPE server_jobqueue_executed_total counter\n";
        oss << "server_jobqueue_executed_total " << _jobQueueExecutedTotal.load() << "\n\n";
        
        oss << "# HELP server_jobqueue_pending_jobs Currently pending jobs in JobQueue\n";
        oss << "# TYPE server_jobqueue_pending_jobs gauge\n";
        oss << "server_jobqueue_pending_jobs " << _jobQueuePendingCount.load() << "\n\n";
        
        // 메모리 메트릭들
        oss << "# HELP server_memory_usage_bytes Memory usage in bytes\n";
        oss << "# TYPE server_memory_usage_bytes gauge\n";
        oss << "server_memory_usage_bytes{type=\"heap\"} " << _memoryUsageBytes.load() << "\n";
        oss << "server_memory_usage_bytes{type=\"pool\"} " << _poolMemoryBytes.load() << "\n\n";
        
        return oss.str();
    }

    // HTTP 서버 시작
    bool Start(int32 port = 10001) {
        if (_running.load()) return false;
        
        _httpPort = port;
        _running = true;
        
        _httpServerThread = std::thread([this]() {
            RunHttpServer();
        });
        
        return true;
    }

    // HTTP 서버 중지
    void Stop() {
        _running = false;
        if (_httpServerThread.joinable()) {
            _httpServerThread.join();
        }
    }

private:
    // 간단한 HTTP 서버 (Windows Winsock 기반)
    void RunHttpServer() {
        WSADATA wsaData;
        if (WSAStartup(MAKEWORD(2,2), &wsaData) != 0) {
            std::cout << "❌ PrometheusMetrics: WSAStartup 실패\n";
            return;
        }

        SOCKET listenSocket = socket(AF_INET, SOCK_STREAM, IPPROTO_TCP);
        if (listenSocket == INVALID_SOCKET) {
            std::cout << "❌ PrometheusMetrics: 소켓 생성 실패\n";
            WSACleanup();
            return;
        }

        sockaddr_in serverAddr{};
        serverAddr.sin_family = AF_INET;
        serverAddr.sin_addr.s_addr = INADDR_ANY;
        serverAddr.sin_port = htons(_httpPort);

        if (::bind(listenSocket, (SOCKADDR*)&serverAddr, sizeof(serverAddr)) == SOCKET_ERROR) {
            std::cout << "❌ PrometheusMetrics: 바인드 실패 (포트 " << _httpPort << ")\n";
            closesocket(listenSocket);
            WSACleanup();
            return;
        }

        if (::listen(listenSocket, SOMAXCONN) == SOCKET_ERROR) {
            std::cout << "❌ PrometheusMetrics: 리슨 실패\n";
            closesocket(listenSocket);
            WSACleanup();
            return;
        }

        std::cout << "📊 PrometheusMetrics HTTP 서버 시작됨 (포트 " << _httpPort << ")\n";

        while (_running.load()) {
            sockaddr_in clientAddr{};
            int clientAddrLen = sizeof(clientAddr);
            
            SOCKET clientSocket = accept(listenSocket, (sockaddr*)&clientAddr, &clientAddrLen);
            if (clientSocket == INVALID_SOCKET) {
                if (_running.load()) {
                    std::cout << "⚠️ PrometheusMetrics: accept 실패\n";
                }
                continue;
            }

            HandleHttpRequest(clientSocket);
            closesocket(clientSocket);
        }

        closesocket(listenSocket);
        WSACleanup();
        std::cout << "📊 PrometheusMetrics HTTP 서버 종료됨\n";
    }

    void HandleHttpRequest(SOCKET clientSocket) {
        char buffer[1024];
        int bytesReceived = recv(clientSocket, buffer, sizeof(buffer) - 1, 0);
        if (bytesReceived <= 0) return;

        buffer[bytesReceived] = '\0';
        std::string request(buffer);

        // 간단한 HTTP 요청 파싱
        if (request.find("GET /metrics") != std::string::npos) {
            std::string metrics = GenerateMetrics();
            std::string response = 
                "HTTP/1.1 200 OK\r\n"
                "Content-Type: text/plain; charset=utf-8\r\n"
                "Content-Length: " + std::to_string(metrics.length()) + "\r\n"
                "Connection: close\r\n"
                "\r\n" + metrics;
            
            send(clientSocket, response.c_str(), static_cast<int>(response.length()), 0);
        } else {
            std::string response = 
                "HTTP/1.1 404 Not Found\r\n"
                "Content-Length: 13\r\n"
                "Connection: close\r\n"
                "\r\n"
                "404 Not Found";
            
            send(clientSocket, response.c_str(), static_cast<int>(response.length()), 0);
        }
    }
}; 