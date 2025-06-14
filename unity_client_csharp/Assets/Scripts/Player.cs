using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private Vector3 _targetPos;
    private Rigidbody _rigid;
    private Vector3 _moveDir = Vector3.zero;
    private float moveSpeed = 15f;
    public ulong PlayerId { get; set; }
    
    void Start()
    {
        _rigid = GetComponent<Rigidbody>();
        SetColor(Color.red);
    }

    private void FixedUpdate()
    {
        _moveDir = (_targetPos - transform.position).normalized;

        if (Vector3.Distance(_targetPos, transform.position) < 0.2)
            return;
        
        Vector3 moveVelocity = _moveDir * moveSpeed;
        _rigid.MovePosition(_rigid.position + moveVelocity * Time.fixedDeltaTime);
    }

    public void OnMovePacket(Protocol.S_BROADCAST_MOVE pkt)
    {
        _targetPos = new Vector3(pkt.PosX, pkt.PosY, pkt.PosZ);
    }

// 여기부터는 Utils
    protected void SetColor(Color color)
    {
        Renderer renderer = GetComponent<Renderer>();
        renderer.material.color = color;
    }
}
