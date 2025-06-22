using System;
using System.Threading;
using System.Threading.Tasks;
using Prometheus;

namespace DummyClientCS
{
    public class PrometheusExporter
    {
        private readonly int _httpPort;
        private readonly string _clientId;
        private MetricServer? _metricServer;
        private CancellationTokenSource? _cancellationTokenSource;
        private bool _isRunning = false;

        // Prometheus 메트릭 정의
        private readonly Counter _totalPacketsSent;
        private readonly Counter _totalPacketsReceived;
        private readonly Counter _movePacketsSent;
        private readonly Counter _chatPacketsSent;
        private readonly Counter _rttPacketsSent;
        
        private readonly Gauge _connectionStatus;
        private readonly Gauge _currentTps;
        private readonly Gauge _avgLatency;
        private readonly Gauge _maxLatency;
        private readonly Gauge _uptime;
        
        private readonly Histogram _latencyHistogram;
        
        // 내부 카운터들 (TPS 계산용)
        private long _recentPacketsSent = 0;
        private long _recentPacketsReceived = 0;
        private DateTime _startTime = DateTime.UtcNow;
        private DateTime _lastTpsUpdate = DateTime.UtcNow;
        private double _currentTpsValue = 0.0;
        private double _avgLatencyValue = 0.0;
        private double _maxLatencyValue = 0.0;

        public PrometheusExporter(int httpPort, string clientId)
        {
            _httpPort = httpPort;
            _clientId = clientId;

            // 메트릭 생성 (라벨 포함)
            _totalPacketsSent = Metrics
                .CreateCounter("client_packets_sent_total", "Total packets sent by client", new[] { "client_id", "type" });
                
            _totalPacketsReceived = Metrics
                .CreateCounter("client_packets_received_total", "Total packets received by client", new[] { "client_id" });
                
            _movePacketsSent = Metrics
                .CreateCounter("client_move_packets_sent_total", "Total move packets sent", new[] { "client_id" });
                
            _chatPacketsSent = Metrics
                .CreateCounter("client_chat_packets_sent_total", "Total chat packets sent", new[] { "client_id" });

            _rttPacketsSent = Metrics
                .CreateCounter("client_rtt_packets_sent_total", "Total RTT packets sent", new[] { "client_id" });

            _connectionStatus = Metrics
                .CreateGauge("client_connection_status", "Connection status (1=connected, 0=disconnected)", new[] { "client_id" });
                
            _currentTps = Metrics
                .CreateGauge("client_tps_current", "Current transactions per second", new[] { "client_id" });
                
            _avgLatency = Metrics
                .CreateGauge("client_latency_avg_ms", "Average latency in milliseconds", new[] { "client_id" });
                
            _maxLatency = Metrics
                .CreateGauge("client_latency_max_ms", "Maximum latency in milliseconds", new[] { "client_id" });
                
            _uptime = Metrics
                .CreateGauge("client_uptime_seconds", "Client uptime in seconds", new[] { "client_id" });

            _latencyHistogram = Metrics
                .CreateHistogram("client_latency_seconds", "Latency histogram in seconds", 
                    new HistogramConfiguration
                    {
                        LabelNames = new[] { "client_id" },
                        Buckets = new[] { 0.001, 0.005, 0.01, 0.025, 0.05, 0.1, 0.25, 0.5, 1.0 }
                    });
        }

        public async Task StartAsync()
        {
            if (_isRunning) return;

            try
            {
                // MetricServer 시작
                _metricServer = new MetricServer(port: _httpPort); // 외부에서 연결 허용해야함
                _metricServer.Start();
                _isRunning = true;

                _cancellationTokenSource = new CancellationTokenSource();
                
                Console.WriteLine($"📊 Client {_clientId} Prometheus MetricServer 시작됨 (localhost:{_httpPort})");
                
                // TPS 업데이트 태스크 시작
                _ = Task.Run(UpdateTpsLoop, _cancellationTokenSource.Token);
                
                // 서버가 시작될 때까지 잠시 대기
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"❌ Client {_clientId} MetricServer 시작 실패 (포트 {_httpPort}): {ex.Message}");
                
                // 포트가 이미 사용 중일 수 있으므로 다른 포트로 시도
                for (int i = 1; i <= 10; i++)
                {
                    int fallbackPort = _httpPort + i;
                    try
                    {
                        _metricServer?.Stop();
                        _metricServer = new MetricServer(port: fallbackPort);
                        _metricServer.Start();
                        _isRunning = true;
                        
                        Console.WriteLine($"📊 Client {_clientId} Prometheus MetricServer 시작됨 (localhost:{fallbackPort}) - 포트 변경됨");
                        _ = Task.Run(UpdateTpsLoop, _cancellationTokenSource.Token);
                        break;
                    }
                    catch (Exception)
                    {
                        if (i == 10)
                        {
                            Console.WriteLine($"❌ Client {_clientId} MetricServer 완전 실패: 사용 가능한 포트를 찾을 수 없습니다");
                        }
                    }
                }
            }
        }

        public void Stop()
        {
            if (!_isRunning) return;

            _isRunning = false;
            _cancellationTokenSource?.Cancel();
            _metricServer?.Stop();
            
            Console.WriteLine($"📊 Client {_clientId} Prometheus MetricServer 종료됨");
        }

        private async Task UpdateTpsLoop()
        {
            while (_isRunning && !_cancellationTokenSource!.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000, _cancellationTokenSource.Token); // 1초마다
                    UpdateMetrics();
                }
                catch (OperationCanceledException)
                {
                    // 정상적인 취소
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ Client {_clientId} 메트릭 업데이트 오류: {ex.Message}");
                }
            }
        }

        private void UpdateMetrics()
        {
            var now = DateTime.UtcNow;
            var elapsed = (now - _lastTpsUpdate).TotalSeconds;
            
            if (elapsed >= 1.0)
            {
                var recentTotal = Interlocked.Exchange(ref _recentPacketsSent, 0) + 
                                 Interlocked.Exchange(ref _recentPacketsReceived, 0);
                _currentTpsValue = recentTotal / elapsed;
                _lastTpsUpdate = now;
            }

            // 메트릭 업데이트
            _currentTps.WithLabels(_clientId).Set(_currentTpsValue);
            _avgLatency.WithLabels(_clientId).Set(_avgLatencyValue);
            _maxLatency.WithLabels(_clientId).Set(_maxLatencyValue);
            _uptime.WithLabels(_clientId).Set((DateTime.UtcNow - _startTime).TotalSeconds);
        }

        // 메트릭 업데이트 메서드들
        public void IncrementPacketsSent()
        {
            _totalPacketsSent.WithLabels(_clientId, "total").Inc();
            Interlocked.Increment(ref _recentPacketsSent);
        }

        public void IncrementPacketsReceived()
        {
            _totalPacketsReceived.WithLabels(_clientId).Inc();
            Interlocked.Increment(ref _recentPacketsReceived);
        }

        public void IncrementMovePackets()
        {
            _movePacketsSent.WithLabels(_clientId).Inc();
            _totalPacketsSent.WithLabels(_clientId, "move").Inc();
        }

        public void IncrementChatPackets()
        {
            _chatPacketsSent.WithLabels(_clientId).Inc();
            _totalPacketsSent.WithLabels(_clientId, "chat").Inc();
        }

        public void IncrementRttPackets()
        {
            _rttPacketsSent.WithLabels(_clientId).Inc();
        }

        public void UpdateLatency(double latencyMs)
        {
            _avgLatencyValue = latencyMs; // 실제로는 이동평균 계산 필요
            if (latencyMs > _maxLatencyValue)
                _maxLatencyValue = latencyMs;

            // 히스토그램 업데이트 (초 단위로 변환)
            _latencyHistogram.WithLabels(_clientId).Observe(latencyMs / 1000.0);
        }

        public void SetConnectionStatus(bool isConnected)
        {
            _connectionStatus.WithLabels(_clientId).Set(isConnected ? 1 : 0);
        }
    }
} 