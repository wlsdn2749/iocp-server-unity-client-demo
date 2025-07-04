using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace DummyClientCS
{
    // 클라이언트 성능 통계 수집 클래스
    public class ClientPerformanceStats
    {
        private static ClientPerformanceStats _instance = new ClientPerformanceStats();
        public static ClientPerformanceStats Instance { get { return _instance; } }

        private long _totalPacketsSent = 0;
        private long _totalPacketsReceived = 0;
        private long _recentPacketsSent = 0;
        private long _recentPacketsReceived = 0;
        
        private DateTime _startTime = DateTime.Now;
        private List<double> _latencies = new List<double>();
        private List<double> _rttLatencies = new List<double>();
        private readonly object _lock = new object();
        
        private Timer _saveTimer;
        private int _processId;
        private bool _enableFileSaving = false;

        private ClientPerformanceStats()
        {
            _processId = Process.GetCurrentProcess().Id;
        }

        public void OnPacketSent()
        {
            Interlocked.Increment(ref _totalPacketsSent);
            Interlocked.Increment(ref _recentPacketsSent);
        }

        public void OnPacketReceived()
        {
            Interlocked.Increment(ref _totalPacketsReceived);
            Interlocked.Increment(ref _recentPacketsReceived);
        }

        public void RecordLatency(double latencyMs)
        {
            lock (_lock)
            {
                _latencies.Add(latencyMs);
                // 최근 1000개만 유지 (메모리 절약)
                if (_latencies.Count > 1000)
                {
                    _latencies.RemoveAt(0);
                }
            }
        }

        public void RecordRtt(double rttMs)
        {
            lock (_lock)
            {
                _rttLatencies.Add(rttMs);
                // 최근 1000개만 유지 (메모리 절약)
                if (_rttLatencies.Count > 1000)
                {
                    _rttLatencies.RemoveAt(0);
                }
            }
        }

        public void StartPeriodicSave(int intervalSeconds = 1)
        {
            _enableFileSaving = true;
            _saveTimer = new Timer(SaveStatsToFile, null, 
                TimeSpan.FromSeconds(intervalSeconds), 
                TimeSpan.FromSeconds(intervalSeconds));
        }

        private void SaveStatsToFile(object state)
        {
            // 파일 저장이 비활성화된 경우 건너뜀
            if (!_enableFileSaving)
                return;
                
            try
            {
                var uptime = DateTime.Now - _startTime;
                double avgLatency = 0.0;
                double maxLatency = 0.0;
                double avgRtt = 0.0;
                double maxRtt = 0.0;

                lock (_lock)
                {
                    if (_latencies.Count > 0)
                    {
                        double sum = 0;
                        maxLatency = _latencies[0];
                        
                        foreach (var latency in _latencies)
                        {
                            sum += latency;
                            if (latency > maxLatency)
                                maxLatency = latency;
                        }
                        avgLatency = sum / _latencies.Count;
                    }
                    
                    if (_rttLatencies.Count > 0)
                    {
                        double sum = 0;
                        maxRtt = _rttLatencies[0];
                        
                        foreach (var rtt in _rttLatencies)
                        {
                            sum += rtt;
                            if (rtt > maxRtt)
                                maxRtt = rtt;
                        }
                        avgRtt = sum / _rttLatencies.Count;
                    }
                }

                double tps = uptime.TotalSeconds > 0 ? 
                    _totalPacketsSent / uptime.TotalSeconds : 0.0;

                var stats = new
                {
                    processId = _processId,
                    timestamp = (long)uptime.TotalMilliseconds,
                    totalPacketsSent = _totalPacketsSent,
                    totalPacketsReceived = _totalPacketsReceived,
                    recentPacketsSent = _recentPacketsSent,
                    recentPacketsReceived = _recentPacketsReceived,
                    tps = tps,
                    avgLatency = avgLatency,
                    maxLatency = maxLatency,
                    avgRtt = avgRtt,
                    maxRtt = maxRtt,
                    uptimeMs = (long)uptime.TotalMilliseconds
                };

                string filename = $"client_stats_{_processId}.json";
                string json = JsonSerializer.Serialize(stats, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(filename, json);

                // 최근 통계 리셋
                Interlocked.Exchange(ref _recentPacketsSent, 0);
                Interlocked.Exchange(ref _recentPacketsReceived, 0);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"통계 저장 실패: {ex.Message}");
            }
        }

        public void Stop()
        {
            _saveTimer?.Dispose();
            
            if (_enableFileSaving)
            {
                SaveStatsToFile(null); // 마지막 저장
                Console.WriteLine($"📊 클라이언트 통계 저장 완료: client_stats_{_processId}.json");
            }
            else
            {
                Console.WriteLine("📊 클라이언트 통계 저장 건너뜀 (파일 저장 비활성화)");
            }
        }
    }
} 