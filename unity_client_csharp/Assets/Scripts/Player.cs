using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
   
    protected Rigidbody _rigid;
    protected Vector3 _moveDir = Vector3.zero;
    protected float moveSpeed = 15f;
    public ulong PlayerId { get; set; }
    private Vector3 _targetPos;
    protected NameTagUI _tag;
    
    /* ---------- NameTag / RTT ---------- */
    public virtual void AttachNameTag(NameTagUI tag)
    {
        _tag = tag;
        
        
    }
    public virtual void SetRtt(double _) { } // 원격 플레이어는 RTT 표시 X

    
    /* ---------- 초기화 ---------- */
    protected virtual void Awake()
    {
        _rigid = GetComponent<Rigidbody>();
    }

    private void FixedUpdate()
    {
        if (Vector3.Distance(_targetPos, transform.position) < 0.2)
            return;
        
        Vector3 moveVelocity = _moveDir * moveSpeed;
        _rigid.MovePosition(_rigid.position + moveVelocity * Time.fixedDeltaTime);
    }

    /* ---------- 원격 MOVE 패킷 처리 ---------- */
    public void OnMovePacket(Protocol.S_BROADCAST_MOVE pkt)
    {
        _targetPos = new Vector3(pkt.PosX, pkt.PosY, pkt.PosZ);
        _moveDir   = (_targetPos - transform.position).normalized;
    }

    // 여기부터는 Utils
    protected void SetColor(Color color)
    {
        GetComponent<Renderer>().material.color = color;
    }
}
