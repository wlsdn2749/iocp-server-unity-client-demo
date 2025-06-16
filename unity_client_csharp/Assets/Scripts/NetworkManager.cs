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

    private ServerSession _session = new ServerSession();

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
        
    }

    // Update is called once per frame
    void Update()
    {

        List<IMessage> packetList = PacketQueue.Instance.PopAll();
        
        foreach(IMessage pkt in packetList)
            ServerPacketManager.Instance.HandlePacket(_session, pkt);
        
    }

    // void OnApplicationQuit()
    // {
    //     Debug.Log("Application Quit - Cleaning up network resources");
    //     if (_session != null)
    //     {
    //         _session.Disconnect();
    //         _session = null;
    //     }
    //     StopAllCoroutines();
    // }
    //
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
