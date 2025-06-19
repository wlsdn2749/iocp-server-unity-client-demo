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

        static void Main(string[] args)
        {
            Console.WriteLine($"🚀 DummyClientCS 시작 (PID: {System.Diagnostics.Process.GetCurrentProcess().Id})");
            
            // 성능 통계 수집 시작
            ClientPerformanceStats.Instance.StartPeriodicSave(1);
            
            // Ctrl+C 핸들러 등록
            Console.CancelKeyPress += (sender, e) =>
            {
                Console.WriteLine("\n🛑 클라이언트 종료 중... 통계 저장");
                ClientPerformanceStats.Instance.Stop();
                
                // 개별 실행 시 통계 파일 삭제 (GTest 환경이 아닌 경우)
                var processId = System.Diagnostics.Process.GetCurrentProcess().Id;
                var statsFile = $"client_stats_{processId}.json";
                try 
                {
                    if (File.Exists(statsFile))
                    {
                        File.Delete(statsFile);
                        Console.WriteLine($"🗑️ {statsFile} 파일 삭제됨 (개별 실행)");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"⚠️ 통계 파일 삭제 실패: {ex.Message}");
                }
                
                Environment.Exit(0);
            };

            //string host = Dns.GetHostName();
            //IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 8421);

            Connector connector = new Connector();

            connector.Connect(endPoint,
                () => { return SessionManager.Instance.Generate(); },
                1); // 기본 1

            //Move 패킷을 주기적으로 보내는 스레드
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        SessionManager.Instance.SendForEachMove();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine($"SendForEachMove Error: {e}");
                    }
                    Thread.Sleep(250);
                }
            });

            // Chat 패킷을 주기적으로 보내는 메인 스레드
            while (true)
            {
                try
                {
                    SessionManager.Instance.SendForEachChat();
                }
                catch (Exception e)
                {
                    Console.WriteLine($"SendForEachChat Error: {e}");
                }
                Thread.Sleep(2000); // 2초마다
            }

            while (true)
            {

            }
        }
    }

}
