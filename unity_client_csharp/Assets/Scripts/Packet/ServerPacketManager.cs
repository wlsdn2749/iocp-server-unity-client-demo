using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading;
using Google.Protobuf;

namespace Packet
{
    public enum PacketID : ushort
    {
        // TODO(AUTOMATION) 이부분 자동화 + 이부분은 S+C전부 만들어줘야함
        PKT_C_LOGIN = 1000,
        PKT_S_LOGIN = 1001,
        PKT_C_ENTER_GAME = 1002,
        PKT_S_ENTER_GAME = 1003,
        PKT_C_CHAT = 1004,
        PKT_S_CHAT = 1005,
    }
    public class ServerPacketManager
    {
        #region Singleton
        static ServerPacketManager _instance = new ServerPacketManager();
        
        public static ServerPacketManager Instance
        {
            get { return _instance; }
        }
        #endregion

        ServerPacketManager()
        {
            Register();
        }
        
        // C++의 배열 방식, C++과 다른점은 저장하는 대리자가 (Session, buffer, len)아니고 (Session, buffer)
        // 그 이유는 len없어도 ArraySegment의 buffer에 len이 담겨있어 추출 가능
        // _handler[Protocol.ID]는 그 Protocol.Id를 처리하는 대리자일 것. 아니면 INVALID_로 처리
        Action<PacketSession, ArraySegment<byte>>[] _packetHandlers = new Action<PacketSession, ArraySegment<byte>>[ushort.MaxValue + 1];
        
        void Register()
        {
            for (int i = 0; i < UInt16.MaxValue + 1; i++)
            {
                _packetHandlers[i] = ServerPacketHandler.HANDLE_INVALID;
            }
            // TODO(AUTOMATION) 이 부분 자동화 해야함 + PKT_S_XXX부분만 하면 됨
            _packetHandlers[(ushort)PacketID.PKT_S_CHAT] = MakeHandler(ServerPacketHandler.HANDLE_S_Chat, Protocol.S_CHAT.Parser);
            // ...이하 반복
        }

        public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer,  Action<PacketSession, IPacket> OnRecvCallback = null)
        {
            ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
            ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);

            Action<PacketSession, ArraySegment<byte>> handler = _packetHandlers[id];
            // TODO onRecvCallBack 에 대한 처리
            handler(session, buffer);

        }

        public static Action<PacketSession, ArraySegment<byte>> MakeHandler<T>(Action<PacketSession, T> action, MessageParser<T> parser)
            where T : IMessage<T>
        {
            return (session, buffer) =>
            {
                ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
                int protoLen = size - 4; // Header Size
                int protoOffset = buffer.Offset + 4; // Header Size
                T pkt = parser.ParseFrom(buffer.Array, protoOffset, protoLen);
                action.Invoke(session, pkt);
            };
        }

    }
}