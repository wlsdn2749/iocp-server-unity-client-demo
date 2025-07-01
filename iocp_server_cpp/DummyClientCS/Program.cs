using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using ServerCore;


namespace DummyClientCS
{

    class Program
    {
        private static PrometheusExporter? prometheusExporter;
        private static CancellationTokenSource cancellationTokenSource = new CancellationTokenSource();

        public static PrometheusExporter? GetPrometheusExporter()
        {
            return prometheusExporter;
        }

        static void Main(string[] args)
        {
            // 명령줄 인수 파싱
            string clientId = System.Diagnostics.Process.GetCurrentProcess().Id.ToString();
            int httpPort = 8081;
            int moveInterval = 250;
            int chatInterval = 2000;
            bool isGTestMode = false;

            for (int i = 0; i < args.Length; i++)
            {
                if (args[i] == "--client-id" && i + 1 < args.Length)
                    clientId = args[i + 1];
                else if ((args[i] == "--http-port" || args[i] == "--prometheus-port") && i + 1 < args.Length)
                    int.TryParse(args[i + 1], out httpPort);
                else if (args[i] == "--move-interval" && i + 1 < args.Length)
                    int.TryParse(args[i + 1], out moveInterval);
                else if (args[i] == "--chat-interval" && i + 1 < args.Length)
                    int.TryParse(args[i + 1], out chatInterval);
                else if (args[i] == "--gtest" || args[i] == "--performance-test")
                    isGTestMode = true;
            }
            
            // 환경 변수로도 gTest 모드 확인
            if (!isGTestMode && (
                Environment.GetEnvironmentVariable("GTEST_MODE") == "true" ||
                Environment.GetEnvironmentVariable("PERFORMANCE_TEST") == "true" ||
                Environment.GetEnvironmentVariable("UNIT_TEST") == "true"))
            {
                isGTestMode = true;
            }

            Console.WriteLine($"🚀 DummyClientCS 시작 (PID: {System.Diagnostics.Process.GetCurrentProcess().Id})");
            Console.WriteLine($"   - Client ID: {clientId}");
            Console.WriteLine($"   - HTTP Port: {httpPort}");
            Console.WriteLine($"   - Move Interval: {moveInterval}ms");
            Console.WriteLine($"   - Chat Interval: {chatInterval}ms");
            Console.WriteLine($"   - gTest Mode: {isGTestMode}");
            
            // 성능 통계 수집 시작 (gTest 모드에서만)
            if (isGTestMode)
            {
                ClientPerformanceStats.Instance.StartPeriodicSave(1);
                Console.WriteLine("📊 성능 통계 수집 시작 (gTest 모드)");
            }
            else
            {
                Console.WriteLine("📊 성능 통계 수집 비활성화 (일반 모드)");
            }

            // Prometheus Exporter 시작
            prometheusExporter = new PrometheusExporter(httpPort, clientId);
            Task.Run(async () => await prometheusExporter.StartAsync());
            
            // Ctrl+C 핸들러 등록
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("\n🛑 클라이언트 종료 중...");
                
                // 취소 토큰 설정
                cancellationTokenSource.Cancel();
                
                // 정리 작업
                if (isGTestMode)
                {
                    Console.WriteLine("📊 통계 저장 중... (gTest 모드)");
                    ClientPerformanceStats.Instance.Stop();
                }
                else
                {
                    Console.WriteLine("📊 통계 저장 건너뜀 (일반 모드)");
                }
                
                prometheusExporter?.Stop();
                
                // 일반 모드에서는 혹시 생성된 통계 파일을 삭제
                if (!isGTestMode)
                {
                    var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                    var statsFile = $"client_stats_{processId}.json";
                    try 
                    {
                        if (File.Exists(statsFile))
                        {
                            File.Delete(statsFile);
                            Console.WriteLine($"🗑️ {statsFile} 파일 삭제됨 (일반 모드)");
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"⚠️ 통계 파일 삭제 실패: {ex.Message}");
                    }
                }
                
                Environment.Exit(0);
            };

            // 서버 연결
            IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 8421);

            Connector connector = new Connector();
            connector.Connect(endPoint,
                () => { 
                    var session = SessionManager.Instance.Generate();
                    prometheusExporter?.SetConnectionStatus(true);
                    return session;
                },
                10);

            // Move 패킷을 주기적으로 보내는 Task
            Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        SessionManager.Instance.SendForEachMove();
                        prometheusExporter?.IncrementMovePackets();
                        prometheusExporter?.IncrementPacketsSent();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"SendForEachMove Error: {e}");
                    }
                    
                    await Task.Delay(moveInterval, cancellationTokenSource.Token);
                }
            }, cancellationTokenSource.Token);

            // Chat 패킷을 주기적으로 보내는 Task
            Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        SessionManager.Instance.SendForEachChat();
                        prometheusExporter?.IncrementChatPackets();
                        prometheusExporter?.IncrementPacketsSent();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"SendForEachChat Error: {e}");
                    }
                    
                    await Task.Delay(chatInterval, cancellationTokenSource.Token);
                }
            }, cancellationTokenSource.Token);

            // RTT 패킷을 1초마다 보내는 Task
            Task.Run(async () =>
            {
                while (!cancellationTokenSource.Token.IsCancellationRequested)
                {
                    try
                    {
                        SessionManager.Instance.SendForEachRtt();
                        prometheusExporter?.IncrementRttPackets();
                        prometheusExporter?.IncrementPacketsSent();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"SendForEachRtt Error: {e}");
                    }
                    
                    await Task.Delay(1000, cancellationTokenSource.Token); // 1초마다
                }
            }, cancellationTokenSource.Token);

            // 메인 스레드는 대기 상태로 유지
            try
            {
                Task.Delay(-1, cancellationTokenSource.Token).Wait();
            }
            catch (OperationCanceledException)
            {
                Console.WriteLine("애플리케이션이 정상적으로 종료되었습니다.");
            }
        }
    }
}
