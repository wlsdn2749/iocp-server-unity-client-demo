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
            //string host = Dns.GetHostName();
            //IPHostEntry ipHost = Dns.GetHostEntry(host);
            IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
            IPEndPoint endPoint = new IPEndPoint(ipAddr, 8421);

            Connector connector = new Connector();

            connector.Connect(endPoint,
                () => { return SessionManager.Instance.Generate(); },
                5);

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
