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

        Instance = this;
        DontDestroyOnLoad(gameObject); // 씬이 바뀌어도 파괴되지 않도록
    }
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private ServerSession _session = new ServerSession();

    public void Send(ArraySegment<byte> sendBuff)
    {
        _session.Send(sendBuff);
    }
    void Start()
    {
        Debug.Log("NetworkSession Start");
        // string host = Dns.GetHostName();
        // IPHostEntry ipHost = Dns.GetHostEntry(host);
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

        // List<IPacket> packetList = PacketQueue.Instance.PopAll();
        List<IMessage> packetList = PacketQueue.Instance.PopAll();
        
        foreach(IMessage pkt in packetList)
            ServerPacketManager.Instance.HandlePacket(_session, pkt);
        
    }


}
