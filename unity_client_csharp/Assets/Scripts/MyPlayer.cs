using System;
using System.Collections;
using Google.Protobuf;
using Packet;
using UnityEngine;
using Protocol;
using UnityEngine.Rendering;

public class MyPlayer : Player
{
    public float moveSpeed = 15f;

    private Rigidbody _rigid;
    private Vector3 _moveDir = Vector3.zero;
    
    void Start() 
    {
        _rigid = GetComponent<Rigidbody>();
        Debug.Log("My Player Start");
        SetColor(Color.green);
        StartCoroutine(nameof(CoSendPacket));
        StartCoroutine(nameof(CoSendMovePacket));
    }

    void FixedUpdate()
    {
        Vector3 moveVelocity = _moveDir * moveSpeed;
        _rigid.MovePosition(_rigid.position + moveVelocity * Time.fixedDeltaTime);
    }

    
    void Update()
    {
        // 1. 입력 받아 이동 방향 설정
        float h = Input.GetAxisRaw("Horizontal"); // A, D
        float v = Input.GetAxisRaw("Vertical");   // W, S

        _moveDir = new Vector3(h, 0, v).normalized;
    }
    IEnumerator CoSendMovePacket()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.05f); // 50ms
            Vector3 pos = transform.position;
            C_MOVE movePacket = new C_MOVE()
            {
                PosX = pos.x,
                PosY = pos.y,
                PosZ = pos.z,
            };
            ArraySegment<byte> sendBuffer = ServerPacketManager.MakeSendBuffer(movePacket);
            NetworkManager.Instance.Send(sendBuffer);
        }
    }
    IEnumerator CoSendPacket()
    {
        while (true)
        {
            Debug.Log("SendPacket!");
            yield return new WaitForSeconds(3.0f);
            // C_CHAT chatPkt = new C_CHAT();
            // chatPkt.Msg = "Hello World !"; // 대소문자 주의: C#은 PascalCase로 자동 변환됨
            // byte[] sendBuffer = chatPkt.ToByteArray();
            //
            // ushort packetId = 1004; /* C_CHAT에 해당하는 패킷 번호, 예: 1004 */;
            // ushort bodySize = (ushort)sendBuffer.Length;
            // ushort totalSize = (ushort)(bodySize + 4); // 헤더 4바이트
            //
            // byte[] finalPacket = new byte[totalSize];
            // Buffer.BlockCopy(BitConverter.GetBytes(totalSize), 0, finalPacket, 0, 2); // size
            // Buffer.BlockCopy(BitConverter.GetBytes(packetId), 0, finalPacket, 2, 2);  // id
            // Buffer.BlockCopy(sendBuffer, 0, finalPacket, 4, bodySize);                // protobuf 본문
            //
            // _network.Send(finalPacket);

            Protocol.C_CHAT chatPkt = new Protocol.C_CHAT()
            {
                Msg = "Hello World!"
            };

            ArraySegment<byte> sendBuffer = ServerPacketManager.MakeSendBuffer(chatPkt);
            NetworkManager.Instance.Send(sendBuffer);
        }
    }
}
