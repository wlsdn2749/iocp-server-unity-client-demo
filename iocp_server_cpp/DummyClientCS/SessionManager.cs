using Protocol;
using Packet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DummyClientCS
{
    internal class SessionManager
    {
        static SessionManager _session = new SessionManager();
        public static SessionManager Instance { get { return _session; } }

        List<ServerSession> _sessions = new List<ServerSession>();
        object _lock = new object();
        Random _rand = new Random();
        bool _canSendPackets = false; // ENTER_GAME 성공 후에만 패킷 송신 허용

        public void SendForEachMove()
        {
            if (!_canSendPackets) return; // ENTER_GAME 완료 전에는 패킷 송신 금지

            lock (_lock)
            {
                foreach (ServerSession session in _sessions)
                {
                    Protocol.C_MOVE movePacket = new Protocol.C_MOVE();
                    movePacket.PosX = _rand.Next(-50, 50);
                    movePacket.PosY = 0;
                    movePacket.PosZ = _rand.Next(-50, 50);
                    ArraySegment<byte> segment = ServerPacketManager.MakeSendBuffer(movePacket);

                    session.Send(segment);
                    ClientPerformanceStats.Instance.OnPacketSent();
                }
            }
        }

        public void SendForEachChat()
        {
            if (!_canSendPackets) return; // ENTER_GAME 완료 전에는 패킷 송신 금지

            lock (_lock)
            {
                foreach (ServerSession session in _sessions)
                {
                    Protocol.C_CHAT chatPacket = new Protocol.C_CHAT();
                    chatPacket.Msg = "Hello World from C# DummyClient!";
                    ArraySegment<byte> segment = ServerPacketManager.MakeSendBuffer(chatPacket);

                    session.Send(segment);
                    ClientPerformanceStats.Instance.OnPacketSent();
                }
            }
        }

        public void SetCanSendPackets(bool canSend)
        {
            _canSendPackets = canSend;
        }
        public ServerSession Generate()
        {
            lock (_lock)
            {
                ServerSession session = new ServerSession();
                _sessions.Add(session);
                return session;
            }
        }
    }
}
