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
        const float Step = Speed * TickSec;     // 0.75 m

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
            if (!_canSendPackets) return;

            lock (_lock)
            {
                foreach (ServerSession session in _sessions)
                {
                    /* ① 현재 위치 확보 (없으면 초기화) ---------------------------- */
                    if (!_posTable.TryGetValue(session, out Vector3 pos))
                        _posTable[session] = pos = new Vector3(0, 10, 0);

                    /* ② 목적지 확보 (없으면 초기화) ------------------------------ */
                    if (!_destTable.TryGetValue(session, out Vector3 dest))
                    {
                        _destTable[session] = dest = RandomPos();
                    }

                    /* ③ 방향·거리 계산 ------------------------------------------ */
                    Vector3 diff = dest - pos;
                    float dist = diff.Length();
                    Vector3 moveDir = (dist > 1e-6f) ? diff / dist : Vector3.UnitZ;

                    /* ④ 한 스텝 이동 ------------------------------------------- */
                    bool arrived = dist <= Step;          // 이번 Tick 에 붙는가?

                    if (arrived)
                    {
                        pos = dest;                       // 목적지 스냅
                        //Console.WriteLine($"dist: {dist}, step: {Step}, arrived:{arrived}");
                    }
                    else
                        pos += moveDir * Step;            // 정상 이동

                    /* ⑤ 도착했다면 새 목적지 설정 ------------------------------- */
                    if (arrived)
                    {
                        dest = RandomPos();
                        //Console.WriteLine($"Target pos: {dest}");
                        _destTable[session] = dest;
                    }
                    _posTable[session] = pos;             // 위치 갱신
                    

                    /* ⑥ 패킷용 dir·speed 계산 ---------------------------------- */
                    Vector3 nextDiff = dest - pos;        // (새) 목적지 기준
                    float nextDist = nextDiff.Length();
                    Vector3 dirSend = (nextDist > 1e-6f) ? nextDiff / nextDist : Vector3.Zero;

                    float speedSend = (dirSend == Vector3.Zero) ? 0f : Speed;

                    /* ⑦ 입력 패킷 전송 ----------------------------------------- */
                    var input = new PlayerMoveInput
                    {
                        Dir = new Vec3 { X = dirSend.X, Y = 0, Z = dirSend.Z },
                        Speed = speedSend
                    };
                    //Console.WriteLine($"Current pos: {pos}, Current Dir = {dirSend}" );
                    var pkt = new C_MOVE { Input = input };

                    session.Send(ServerPacketManager.MakeSendBuffer(pkt));
                    ClientPerformanceStats.Instance?.OnPacketSent();
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
