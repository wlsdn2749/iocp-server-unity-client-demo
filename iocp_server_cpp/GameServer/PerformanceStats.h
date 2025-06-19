#pragma once
#include "CorePch.h"
#include <atomic>
#include <chrono>
#include <fstream>
#include <sstream>

// 서버 성능 통계 수집 클래스
class PerformanceStats
{
public:
    static PerformanceStats* Instance()
    {
        static PerformanceStats instance;
        return &instance;
    }

    // 패킷 통계
    void OnPacketReceived() 
    { 
        _totalPacketsReceived++; 
        _recentPacketsReceived++;
    }
    
    void OnPacketSent() 
    { 
        _totalPacketsSent++; 
        _recentPacketsSent++;
    }

    // 클라이언트 연결 통계
    void OnClientConnected() 
    { 
        _connectedClients++;
        _totalConnections++;
    }
    
    void OnClientDisconnected() 
    { 
        if (_connectedClients > 0) 
            _connectedClients--;
    }

    // 패킷 처리 시간 기록
    void RecordPacketProcessTime(double processingTimeMs)
    {
        double current = _totalProcessingTime.load();
        while (!_totalProcessingTime.compare_exchange_weak(current, current + processingTimeMs)) {
            // Retry until successful
        }
        _processedPackets++;
        
        double currentMax = _maxProcessingTime.load();
        while (processingTimeMs > currentMax && 
               !_maxProcessingTime.compare_exchange_weak(currentMax, processingTimeMs)) {
            // Retry until successful
        }
    }

    // 통계를 JSON 파일로 저장
    void SaveStatsToFile()
    {
        auto now = std::chrono::high_resolution_clock::now();
        auto duration = std::chrono::duration_cast<std::chrono::milliseconds>(now - _startTime);
        
        double avgProcessingTime = _processedPackets > 0 ? 
            _totalProcessingTime / _processedPackets : 0.0;
        
        double tps = duration.count() > 0 ? 
            (_totalPacketsReceived * 1000.0) / duration.count() : 0.0;

        std::ofstream file("server_stats.json");
        if (file.is_open())
        {
            file << "{\n";
            file << "  \"timestamp\": " << duration.count() << ",\n";
            file << "  \"connectedClients\": " << _connectedClients << ",\n";
            file << "  \"totalConnections\": " << _totalConnections << ",\n";
            file << "  \"totalPacketsReceived\": " << _totalPacketsReceived << ",\n";
            file << "  \"totalPacketsSent\": " << _totalPacketsSent << ",\n";
            file << "  \"recentPacketsReceived\": " << _recentPacketsReceived << ",\n";
            file << "  \"recentPacketsSent\": " << _recentPacketsSent << ",\n";
            file << "  \"tps\": " << tps << ",\n";
            file << "  \"avgProcessingTime\": " << avgProcessingTime << ",\n";
            file << "  \"maxProcessingTime\": " << _maxProcessingTime << ",\n";
            file << "  \"uptimeMs\": " << duration.count() << "\n";
            file << "}\n";
            file.close();
        }

        // 최근 통계 리셋 (다음 주기를 위해)
        _recentPacketsReceived = 0;
        _recentPacketsSent = 0;
    }

    // 주기적 통계 저장 시작
    void StartPeriodicSave(int intervalSeconds = 1)
    {
        _running = true;
        _statsThread = std::thread([this, intervalSeconds]() {
            while (_running)
            {
                std::this_thread::sleep_for(std::chrono::seconds(intervalSeconds));
                if (_running)
                    SaveStatsToFile();
            }
        });
    }

    void Stop()
    {
        _running = false;
        if (_statsThread.joinable())
            _statsThread.join();
        
        // 마지막 통계 저장
        SaveStatsToFile();
    }

private:
    PerformanceStats() : _startTime(std::chrono::high_resolution_clock::now()) {}
    ~PerformanceStats() { Stop(); }

    std::atomic<int> _connectedClients{0};
    std::atomic<int> _totalConnections{0};
    std::atomic<long long> _totalPacketsReceived{0};
    std::atomic<long long> _totalPacketsSent{0};
    std::atomic<long long> _recentPacketsReceived{0};
    std::atomic<long long> _recentPacketsSent{0};
    
    std::atomic<double> _totalProcessingTime{0.0};
    std::atomic<long long> _processedPackets{0};
    std::atomic<double> _maxProcessingTime{0.0};

    std::chrono::high_resolution_clock::time_point _startTime;
    std::atomic<bool> _running{false};
    std::thread _statsThread;
};

// 매크로로 사용 편의성 제공
#define STATS_PACKET_RECEIVED() PerformanceStats::Instance()->OnPacketReceived()
#define STATS_PACKET_SENT() PerformanceStats::Instance()->OnPacketSent()
#define STATS_CLIENT_CONNECTED() PerformanceStats::Instance()->OnClientConnected()
#define STATS_CLIENT_DISCONNECTED() PerformanceStats::Instance()->OnClientDisconnected()
#define STATS_RECORD_PROCESSING_TIME(ms) PerformanceStats::Instance()->RecordPacketProcessTime(ms) 