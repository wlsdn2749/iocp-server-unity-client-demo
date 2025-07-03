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
            yield return new WaitForSeconds(0.25f); // 250ms
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
