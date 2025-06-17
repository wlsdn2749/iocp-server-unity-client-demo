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
    
    public class ServerPacketHandler : MonoBehaviour
    {
        public static void HANDLE_INVALID(PacketSession session, IMessage packet)
        {
            Debug.Log("[패킷 처리] INVALID PACKET (알수 없는 패킷 ID 요청!)");
        }

        
        // (CLIENT) C_LOGIN -> (SERVER) S_LOGIN에 응답하기 위한 패킷
        public static void HANDLE_S_LOGIN(PacketSession session, S_LOGIN packet)
        {
            S_LOGIN pkt = packet as S_LOGIN;
            ServerSession serverSession = session as ServerSession;
            PlayerManager.Instance.Login(pkt);

        }
        public static void HANDLE_S_ENTER_GAME(PacketSession session, S_ENTER_GAME packet)
        {
            Debug.Log("[패킷 처리] HANDLE_S_ENTER_GAME PACKET (SERVER가 S_ENTER_GAME 요청을 보냈음)");
        }
        public static void HANDLE_S_BROADCAST_CHAT(PacketSession session, S_BROADCAST_CHAT packet)
        {
            S_BROADCAST_CHAT pkt = packet as S_BROADCAST_CHAT;
            ServerSession serverSession = session as ServerSession;
            
            UnityEngine.Debug.Log($"[Server's Broadcast] {pkt.PlayerId}: {pkt.Msg}");
            UI.ChatUIManager.Instance?.AddMessage(pkt.PlayerId.ToString(), pkt.Msg);
        }
        public static void HANDLE_S_PLAYERLIST(PacketSession session, S_PLAYERLIST packet)
        {
            S_PLAYERLIST pkt = packet as S_PLAYERLIST;
            ServerSession serverSession = session as ServerSession;
            PlayerManager.Instance.Add(pkt);
        }
        public static void HANDLE_S_BROADCAST_ENTER_GAME(PacketSession session, S_BROADCAST_ENTER_GAME packet)
        {
            S_BROADCAST_ENTER_GAME pkt = packet as S_BROADCAST_ENTER_GAME;
            ServerSession serverSession = session as ServerSession;
            PlayerManager.Instance.EnterGame(pkt);
        }
        public static void HANDLE_S_BROADCAST_MOVE(PacketSession session, S_BROADCAST_MOVE packet)
        {
            S_BROADCAST_MOVE pkt = packet as S_BROADCAST_MOVE;
            ServerSession serverSession = session as ServerSession;
            PlayerManager.Instance.Move(pkt);
        }
    }
}