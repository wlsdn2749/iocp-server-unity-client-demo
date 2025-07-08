using Packet;
using Protocol;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace DummyClientCS
{
    internal class SessionManager
    {
        /* ─── 싱글톤 ────────────────────────────────────────── */
        static SessionManager _session = new SessionManager();
        public static SessionManager Instance { get { return _session; } }

        /* ─── 상수 ────────────────────────────────────────── */
        Random _rand = new Random();
        const float TickSec = 0.05f; // 20 Hz
        const float Speed = 15.0f;  // 15m/s

        /* ─── 상태 테이블 ────────────────────────────────────────── */
        List<ServerSession> _sessions = new List<ServerSession>();
        readonly Dictionary<ServerSession, Vector3> _posTable = new(); // 세션별 좌표
        readonly Dictionary<ServerSession, Vector3> _destTable = new();   // 목적지
        object _lock = new object();

        /* ─── 플래그 ────────────────────────────────────────── */
        bool _canSendPackets = false; // ENTER_GAME 성공 후에만 패킷 송신 허용

        /* ─── 유틸 ────────────────────────────────────────── */
        Vector3 RandomPos() => new Vector3(_rand.Next(-80, 80), 10, _rand.Next(-80, 80));

        public void SendForEachMove()
        {
            if (!_canSendPackets) return; // ENTER_GAME 완료 전에는 패킷 송신 금지

            lock (_lock)
            {
               
                foreach (var session in _sessions) 
                {
                    /* ① 세션별 현재 좌표 꺼내기 (없으면 랜덤 초기화) */
                    // DummyClient는 Session으로 관리하지 Player가 없으므로, 위치만 대신 관리
                    if (!_posTable.TryGetValue(session, out Vector3 pos))
                    {
                        pos = RandomPos();            // (-100,100) 범위
                        _posTable[session] = pos;
                    }


                    /* ② 목적지 없거나 도착했으면 새 방향 설정 */
                    // _destTable에 저장해 둔 목적지가 없거나,
                    // 현 위치(pos)에서 0.75m(step)보다 살짝 큰 0.8m(임계치) 이내로 다가가면
                    // “도착”으로 판정하고 새 목적지를 랜덤으로 잡습니다.
                    if (!_destTable.TryGetValue(session, out Vector3 dest) ||
                        Vector3.Distance(pos, dest) < 0.8f)
                    {
                        dest = RandomPos();
                        _destTable[session] = dest;
                    }

                    /* ─────────── ③ 방향 계산 & 1스텝 이동 ─────────── */
                    // 방향 벡터(dir)는 (dest - pos)의 정규화.
                    // step = 15 m/s × 0.05 s = 0.75 m.
                    // 남은 거리가 step보다 작으면 “이번 틱에 목표지점까지 붙어서”
                    // 좌표를 딱 dest로 맞추고, 다음 틱부터 새 목적지를 향해 이동합니다.
                    Vector3 diff = dest - pos;
                    float dist = diff.Length();
                    Vector3 dir = (dist > 1e-6f) ? diff / dist : Vector3.UnitZ; // 방향을 구하는데, 못구하면 안전하게 Z로
                    float step = Speed * TickSec;

                    // Console.WriteLine("dest: "+ dest, "pos" + pos + "dist: " + dist + " " + "step: " + step);
                    if (dist <= step)              // 도착·오버슈트 처리
                    {
                        pos = dest;
                        dest = RandomPos();
                        _destTable[session] = dest;
                    }
                    else
                    {
                        pos += dir * step;
                    }
                    _posTable[session] = pos;


                    /* 4. dir + speed 패킷 전송 */
                    var input = new PlayerMoveInput
                    {
                        Dir = new Vec3
                        {
                            X = dir.X,
                            Y = dir.Y,
                            Z = dir.Z
                        },
                        Speed = Speed
                    };

                    C_MOVE pkt = new C_MOVE
                    {                      
                        Input = input,
                    };
                    ArraySegment<byte> segment = ServerPacketManager.MakeSendBuffer(pkt);

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

        public void SendForEachRtt()
        {
            if (!_canSendPackets) return; // ENTER_GAME 완료 전에는 패킷 송신 금지

            lock (_lock)
            {
                foreach (ServerSession session in _sessions)
                {
                    Protocol.C_RTT rttPacket = new Protocol.C_RTT();
                    
                    // 현재 클라이언트 시간을 마이크로초 단위로 측정
                    var now = DateTime.UtcNow;
                    var ticks = now.Ticks;
                    var microseconds = ticks / 10; // Ticks를 마이크로초로 변환 (1 tick = 100 nanoseconds)
                    
                    rttPacket.ClientTime = (ulong)microseconds;
                    ArraySegment<byte> segment = ServerPacketManager.MakeSendBuffer(rttPacket);

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
