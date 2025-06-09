using System;
using System.Collections;
using Google.Protobuf;
using UnityEngine;
using Protocol;

public class MyPlayer : Player
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    private NetworkManager _network;
    void Start()
    {
        Debug.Log("My Player Start");
        StartCoroutine(nameof(CoSendPacket));
        _network = GameObject.Find("NetworkManager").GetComponent<NetworkManager>();
    }

    // Update is called once per frame
    void Update()
    {

    }
    
    IEnumerator CoSendPacket()
    {
        while (true)
        {
            Debug.Log("SendPacket!");
            yield return new WaitForSeconds(3.0f);
            C_CHAT chatPkt = new C_CHAT();
            chatPkt.Msg = "Hello World !"; // 대소문자 주의: C#은 PascalCase로 자동 변환됨
            byte[] sendBuffer = chatPkt.ToByteArray();

            ushort packetId = 1004; /* C_CHAT에 해당하는 패킷 번호, 예: 1004 */;
            ushort bodySize = (ushort)sendBuffer.Length;
            ushort totalSize = (ushort)(bodySize + 4); // 헤더 4바이트

            byte[] finalPacket = new byte[totalSize];
            Buffer.BlockCopy(BitConverter.GetBytes(totalSize), 0, finalPacket, 0, 2); // size
            Buffer.BlockCopy(BitConverter.GetBytes(packetId), 0, finalPacket, 2, 2);  // id
            Buffer.BlockCopy(sendBuffer, 0, finalPacket, 4, bodySize);                // protobuf 본문
            
            // C_Move movePacket = new C_Move();
            // movePacket.posX = UnityEngine.Random.Range(-50, 50);
            // movePacket.posY = 0;
            // movePacket.posZ = UnityEngine.Random.Range(-50, 50);
            
            _network.Send(finalPacket);

            // _network.Send(movePacket.Write());
        }
    }
}
