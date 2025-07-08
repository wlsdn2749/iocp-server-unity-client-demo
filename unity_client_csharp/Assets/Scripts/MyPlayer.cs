using System;
using System.Collections;
using Google.Protobuf;
using Packet;
using UnityEngine;
using UnityEngine.Rendering;

public class MyPlayer : Player
{
    [Header("Move Settings")]
    [SerializeField] private float moveSpeed = 15f;
    [SerializeField] private float sendPeriod = 0.05f;

    private Vector3 _localDir = Vector3.zero;
    
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

        _localDir = new Vector3(h, 0, v).normalized;
    }
    
    void FixedUpdate()
    {
        Vector3 next = _rigid.position
                       + _localDir * (moveSpeed * Time.fixedDeltaTime);
        _rigid.MovePosition(next);
    }
    
    /* ---------- RTT ---------- */
    public override void SetRtt(double rtt) => _tag?.UpdateRtt(rtt);
    IEnumerator CoSendMovePacket()
    {
        while (true)
        {
            yield return new WaitForSeconds(sendPeriod); // 50ms
            
            Protocol.PlayerMoveInput input = new Protocol.PlayerMoveInput
            {
                Speed = (_localDir.sqrMagnitude > 0.0001f) ? moveSpeed : 0f,
                Dir   = new Protocol.Vec3
                {
                    X = _localDir.x,
                    Y = _localDir.y,
                    Z = _localDir.z
                }
            };

            Protocol.C_MOVE movePkt = new Protocol.C_MOVE { Input = input };
            ArraySegment<byte> sendBuffer = ServerPacketManager.MakeSendBuffer(movePkt);
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
