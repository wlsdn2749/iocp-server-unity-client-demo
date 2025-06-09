using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using DummyClient;
using ServerCore;
using UnityEngine;

public class NetworkManager : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private ServerSession _session = new ServerSession();

    public void Send(ArraySegment<byte> sendBuff)
    {
        _session.Send(sendBuff);
    }
    void Start()
    {
        string host = Dns.GetHostName();
        IPHostEntry ipHost = Dns.GetHostEntry(host);
        IPAddress ipAddr = ipHost.AddressList[0];
        IPEndPoint endPoint = new IPEndPoint(ipAddr, 8888);

        Connector connector = new Connector();

        connector.Connect(endPoint,
            () => { return _session; },
            1);
        
    }

    // Update is called once per frame
    void Update()
    {

        List<IPacket> packetList = PacketQueue.Instance.PopAll();
        
        foreach(IPacket pkt in packetList)
            PacketManager.Instance.HandlePacket(_session, pkt);
        
    }


}
