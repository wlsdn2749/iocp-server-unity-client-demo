using DummyClient;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Google.Protobuf;
using Protocol;
using UnityEngine;

namespace Packet
{
    public class ServerPacketHandler
    {
        public static void S_BroadcastEnterGameHandler(PacketSession session, IPacket packet)
        {
            S_BroadcastEnterGame pkt = packet as S_BroadcastEnterGame;
            ServerSession serverSession = session as ServerSession;
            PlayerManager.Instance.EnterGame(pkt);
        }

        public static void S_BroadCastLeaveGameHandler(PacketSession session, IPacket packet)
        {
            S_BroadCastLeaveGame pkt = packet as S_BroadCastLeaveGame;
            ServerSession serverSession = session as ServerSession;
            PlayerManager.Instance.LeaveGame(pkt);
        }

        public static void S_PlayerListHandler(PacketSession session, IPacket packet)
        {
            S_PlayerList pkt = packet as S_PlayerList;
            ServerSession serverSession = session as ServerSession;
        
            PlayerManager.Instance.Add(pkt);
        }

        public static void S_BroadCastMoveHandler(PacketSession session, IPacket packet)
        {
            S_BroadCastMove pkt = packet as S_BroadCastMove;
            ServerSession serverSession = session as ServerSession;
            PlayerManager.Instance.Move(pkt);
        }

        public static void HANDLE_S_Chat(PacketSession session, IMessage packet)
        {
            S_CHAT pkt = packet as S_CHAT;
            ServerSession serverSession = session as ServerSession;
            
            UnityEngine.Debug.Log($"[ServerPacketVer.채팅] {pkt.PlayerId}: {pkt.Msg}");
        }
        public static void HANDLE_INVALID(PacketSession session, ArraySegment<byte> buffer)
        {
            Debug.Log("[패킷 처리] INVALID PACKET (알수 없는 패킷 ID 요청!)");
        }

    }
}