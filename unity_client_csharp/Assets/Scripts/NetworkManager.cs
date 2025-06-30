using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using DummyClient;
using Google.Protobuf;
using Packet;
using ServerCore;
using Unity.Collections;
using UnityEngine;
using System.Net.Sockets;

public class NetworkManager : MonoBehaviour
{
    public static NetworkManager Instance { get; private set; }
    private ServerSession _session = new ServerSession();

    void Awake()
    {
        // 중복 인스턴스 방지
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // OnDestoryLoad ... 씬 바뀔때 사라지지 않게 처리하는 것도 필요할 수 있음.
        Instance = this;
    }

    

    public void Send(ArraySegment<byte> sendBuff)
    {
        _session.Send(sendBuff);
    }
    void Start()
    {
        Debug.Log("NetworkSession Start");
        IPAddress ipAddr = IPAddress.Parse("127.0.0.1");
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 8421);

        Connector connector = new Connector();

        connector.Connect(endPoint,
            () => { return _session; },
            1);
        
        // RTT 패킷을 1초마다 보내는 코루틴 시작
        StartCoroutine(SendRttCoroutine());
    }

    // RTT 패킷을 주기적으로 보내는 코루틴
    private IEnumerator SendRttCoroutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(1.0f); // 1초마다
            
            try
            {
                Protocol.C_RTT rttPacket = new Protocol.C_RTT();
                
                // 현재 클라이언트 시간을 마이크로초 단위로 측정
                var now = DateTime.UtcNow;
                var ticks = now.Ticks;
                var microseconds = ticks / 10; // Ticks를 마이크로초로 변환 (1 tick = 100 nanoseconds)
                
                rttPacket.ClientTime = (ulong)microseconds;
                var sendBuffer = ServerPacketManager.MakeSendBuffer(rttPacket);
                _session.Send(sendBuffer);
                
                Debug.Log("[RTT 패킷 전송]");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"RTT 패킷 전송 오류: {e.Message}");
            }
        }
    }

    // Update is called once per frame
    void Update()
    {

        List<IMessage> packetList = PacketQueue.Instance.PopAll();
        
        foreach(IMessage pkt in packetList)
            ServerPacketManager.Instance.HandlePacket(_session, pkt);
        
    }

    void OnApplicationQuit()
    {
        Protocol.C_LEAVE_GAME leave = new Protocol.C_LEAVE_GAME();
        var send = ServerPacketManager.MakeSendBuffer(leave);
        _session.Send(send);

        // 평문 TCP라면 바로 Close 하면 패킷이 유실될 수 있으므로
        // 100ms 정도 WaitForEndOfFrame 또는 Thread.Sleep 후 Close 추천
        _session.Disconnect();
    }

    
    // void OnDestroy()
    // {
    //     Debug.Log("NetworkManager Destroy - Cleaning up network resources");
    //     if (_session != null)
    //     {
    //         _session.Disconnect();
    //         _session = null;
    //     }
    // }
}
