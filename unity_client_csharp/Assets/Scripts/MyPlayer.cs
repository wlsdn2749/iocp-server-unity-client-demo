using System;
using System.Collections;
using Google.Protobuf;
using Packet;
using UnityEngine;
using UnityEngine.Rendering;

public class MyPlayer : Player
{
    /* ---------- 초기화 ---------- */
    protected override void Awake()
    {
        base.Awake();
        StartCoroutine(nameof(CoSendChatPacket));
        StartCoroutine(nameof(CoSendMovePacket));
    }
    
    
    void Update()
    {
        // 1. 입력 받아 이동 방향 설정
        float h = Input.GetAxisRaw("Horizontal"); // A, D
        float v = Input.GetAxisRaw("Vertical");   // W, S

        _moveDir = new Vector3(h, 0, v).normalized;
    }
    
    /* ---------- RTT ---------- */
    public override void SetRtt(double rtt) => _tag?.UpdateRtt(rtt);
    IEnumerator CoSendMovePacket()
    {
        while (true)
        {
            yield return new WaitForSeconds(0.25f); // 250ms
            Vector3 pos = transform.position;
            Protocol.C_MOVE movePacket = new Protocol.C_MOVE()
            {
                PosX = pos.x,
                PosY = pos.y,
                PosZ = pos.z,
            };
            ArraySegment<byte> sendBuffer = ServerPacketManager.MakeSendBuffer(movePacket);
            NetworkManager.Instance.Send(sendBuffer);
        }
    }
    IEnumerator CoSendChatPacket()
    {
        while (true)
        {
            // Debug.Log("SendPacket!");
            yield return new WaitForSeconds(2.0f);
            
            Protocol.C_CHAT chatPkt = new Protocol.C_CHAT()
            {
                Msg = "Hello World!"
            };

            ArraySegment<byte> sendBuffer = ServerPacketManager.MakeSendBuffer(chatPkt);
            NetworkManager.Instance.Send(sendBuffer);
        }
    }
}
