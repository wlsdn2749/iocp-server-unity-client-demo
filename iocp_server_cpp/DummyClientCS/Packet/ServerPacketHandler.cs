using DummyClientCS;
using Google.Protobuf;
using Microsoft.VisualBasic;
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
            // enum 형 따로 변수에 담아두면 보기 편함
            var result = packet.Result;

            switch (result)
            {
                case LoginResult.LoginEmailNotFound:
                    Console.WriteLine("🔑 로그인 실패: 가입되지 않은 이메일입니다.");
                    return;

                case LoginResult.LoginPwMismatch:
                    Console.WriteLine("🔑 로그인 실패: 비밀번호 불일치.");
                    return;

                case LoginResult.LoginServerError:
                    Console.WriteLine("🔑 로그인 실패: 서버 내부 오류.");
                    return;

                case LoginResult.LoginDefaultError:
                    Console.WriteLine("로그인 실패: 기본 에러.");
                    return;

                case LoginResult.LoginSuccess:
                    Console.WriteLine("✅ 로그인 성공!");
                    if (packet.Players.Count == 0)
                    {
                        Console.WriteLine("👤 캐릭터가 없어서 생성 창으로 이동해야 합니다.");
                        return; // 캐릭터 먼저 만들고 나가므로 아래 로직 스킵
                    }
                    break;

                default:
                    Console.WriteLine($"⚠️ 알 수 없는 결과 코드: {(int)result}");
                    return; // 예측 못 한 코드면 입장 차단
            }

            // --- 성공 & 캐릭터 존재 → 게임 입장 패킷 전송 -----------------
            var enterGamePkt = new C_ENTER_GAME
            {
                PlayerIndex = 0 // 첫 번째 캐릭터 *고정*
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

        internal static void HANDLE_S_RTT(PacketSession session, S_RTT packet)
        {
            S_RTT pkt = packet as S_RTT;
            ServerSession serverSession = session as ServerSession;
            
            // 현재 클라이언트 시간을 마이크로초 단위로 측정
            var now = DateTime.UtcNow;
            var ticks = now.Ticks;
            var currentMicroseconds = ticks / 10; // Ticks를 마이크로초로 변환 (1 tick = 100 nanoseconds)
            
            // RTT 계산 (현재 시간 - 원래 클라이언트 전송 시간)
            ulong originalClientTime = pkt.ClientTime;
            ulong currentTime = (ulong)currentMicroseconds;
            
            // RTT = 현재 시간 - 원래 전송 시간 (마이크로초)
            double rttMicroseconds = (double)(currentTime - originalClientTime);
            double rttMs = rttMicroseconds / 1000.0; // 마이크로초를 밀리초로 변환
            
            // 비정상적인 값 필터링 (음수이거나 너무 큰 값)
            if (rttMs < 0 || rttMs > 10000) // 10초 이상은 비정상
            {
                Console.WriteLine($"[RTT 측정] 비정상적인 RTT 값: {rttMs:F2}ms - 무시됨");
                return;
            }
            
            // Prometheus 메트릭 업데이트
            var prometheusExporter = DummyClientCS.Program.GetPrometheusExporter();
            if (prometheusExporter != null)
            {
                prometheusExporter.UpdateLatency(rttMs);
                prometheusExporter.IncrementPacketsReceived();
            }
            
            // 성능 통계 업데이트
            ClientPerformanceStats.Instance?.RecordRtt(rttMs);
            
            Console.WriteLine($"[RTT 측정] {rttMs:F2}ms");
        }

        internal static void HANDLE_S_REGISTER(PacketSession session, S_REGISTER packet)
        {
            S_REGISTER pkt = packet as S_REGISTER;
            ServerSession serverSession = session as ServerSession;
            Console.WriteLine($"[패킷 처리] HANDLE_S_REGISTER PACKET - accountId: {pkt.AccountId} - result: {pkt.Result}");

            //TODO logic
        }
    }
}