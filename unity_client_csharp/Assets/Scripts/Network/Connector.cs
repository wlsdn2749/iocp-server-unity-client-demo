using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace ServerCore
{
    public class Connector
    {
        Func<Session> _sessionFactory;
        public void Connect(IPEndPoint endPoint, Func<Session> sessionFactory, int count = 1)
        {

            for (int i = 0; i< count; i++)
            {
                _sessionFactory += sessionFactory;
                // 휴대폰 설정
                Socket socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                SocketAsyncEventArgs connArgs = new SocketAsyncEventArgs();
                connArgs.Completed += OnConnectCompleted;
                connArgs.RemoteEndPoint = endPoint;
                connArgs.UserToken = socket;
                RegisterConnect(connArgs);
            }
            
        }

        void RegisterConnect(SocketAsyncEventArgs args)
        {
            // as의 경우 Socket type이 아닌 경우 null 발생
            Socket socket = args.UserToken as Socket;
            if (socket == null)
                return;
            bool pending = socket.ConnectAsync(args);
            if(pending == false) // 바로 연결가능 한경우
            {
                OnConnectCompleted(null, args);
            }
        }

        void OnConnectCompleted(object Sender, SocketAsyncEventArgs args)
        {
            if (args.SocketError == SocketError.Success)
            {
                // 연결이 완료 된경우
                try
                {
                    Console.WriteLine($"Connected To {args.RemoteEndPoint.ToString()}");

                    Session session = _sessionFactory.Invoke();
                    session.Start(args.ConnectSocket);
                    session.OnConnected(args.RemoteEndPoint);
                }
                catch (Exception e)
                {
                    Console.WriteLine($"OnConnectCompleted Exception: {e}");
                }
            }
            
        }
    }
}
