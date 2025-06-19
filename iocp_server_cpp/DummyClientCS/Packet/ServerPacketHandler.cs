using DummyClientCS;
using Google.Protobuf;
using Protocol;
using ServerCore;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Packet
{

    public class ServerPacketHandler
    {
        public static void HANDLE_INVALID(PacketSession session, IMessage packet)
        {
            Console.WriteLine("[패킷 처리] INVALID PACKET (알수 없는 패킷 ID 요청!)");
        }


        // (CLIENT) C_LOGIN -> (SERVER) S_LOGIN에 응답하기 위한 패킷
        public static void HANDLE_S_LOGIN(PacketSession session, S_LOGIN packet)
        {
            S_LOGIN pkt = packet as S_LOGIN;

            if (pkt.Success == false)
                // 실패 했음

            if (pkt.Players.Count == 0)
            {
                // 캐릭터 생성창으로 이동
            }
            Protocol.C_ENTER_GAME enterGamePkt = new Protocol.C_ENTER_GAME()
            {
                PlayerIndex = 0, // 첫번째 캐릭터로 입장.
            };

            var sendBuffer = ServerPacketManager.MakeSendBuffer(enterGamePkt);
            session.Send(sendBuffer);

        }
        public static void HANDLE_S_ENTER_GAME(PacketSession session, S_ENTER_GAME packet)
        {
            S_ENTER_GAME pkt = packet as S_ENTER_GAME;
            Console.WriteLine($"[패킷 처리] HANDLE_S_ENTER_GAME PACKET - Success: {pkt.Success}");
            
            if (pkt.Success)
            {
                // 게임 입장 성공 - 이제 Move/Chat 패킷 송신 허용
                SessionManager.Instance.SetCanSendPackets(true);
                Console.WriteLine("[게임 입장 성공] Move/Chat 패킷 송신 시작!");
            }
        }
        public static void HANDLE_S_BROADCAST_CHAT(PacketSession session, S_BROADCAST_CHAT packet)
        {
            S_BROADCAST_CHAT pkt = packet as S_BROADCAST_CHAT;
            ServerSession serverSession = session as ServerSession;

            //Console.WriteLine($"[Server's Broadcast] {pkt.PlayerId}: {pkt.Msg}");
            //UI.ChatUIManager.Instance?.AddMessage(pkt.PlayerId.ToString(), pkt.Msg);
        }
        public static void HANDLE_S_PLAYERLIST(PacketSession session, S_PLAYERLIST packet)
        {
            S_PLAYERLIST pkt = packet as S_PLAYERLIST;
            ServerSession serverSession = session as ServerSession;
            //PlayerManager.Instance.Add(pkt);
        }
        public static void HANDLE_S_BROADCAST_ENTER_GAME(PacketSession session, S_BROADCAST_ENTER_GAME packet)
        {
            S_BROADCAST_ENTER_GAME pkt = packet as S_BROADCAST_ENTER_GAME;
            ServerSession serverSession = session as ServerSession;
            //PlayerManager.Instance.EnterGame(pkt);
        }
        public static void HANDLE_S_BROADCAST_MOVE(PacketSession session, S_BROADCAST_MOVE packet)
        {
            S_BROADCAST_MOVE pkt = packet as S_BROADCAST_MOVE;
            ServerSession serverSession = session as ServerSession;
            //PlayerManager.Instance.Move(pkt);
        }
        public static void HANDLE_S_BROADCAST_LEAVE_GAME(PacketSession session, S_BROADCAST_LEAVE_GAME packet)
        {
            S_BROADCAST_LEAVE_GAME pkt = packet as S_BROADCAST_LEAVE_GAME;
            ServerSession serverSession = session as ServerSession;
            //PlayerManager.Instance.LeaveGame(pkt);
        }
    }
}