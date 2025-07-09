using System;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    // 설정 값
    [Header("Interpolation")]
    [Tooltip("서버 좌표와 10m 이상 차이나면 즉시 스냅")]
    [SerializeField] private float snapDist = 10f;

    [Tooltip("보간 가중치 (값이 클 수록 빠르게 수렴")]
    [SerializeField] private float lerpFactor = 10f;
    
    [Header("Server SnapShot States")]
    [Tooltip("서버로 부터 받은 상태")]
    private Vector3 _serverPos; // 최근 서버 좌표
    private Vector3 _dir;       // 단위 벡터
    private float   _speed;     // m/s
    private float   _lastRecvTime; // Time.time 기준
    
    
    // 캐시, UI 식별 ...
    protected Rigidbody _rigid;
    public ulong PlayerId { get; set; }
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
        float dt = Time.time - _lastRecvTime;
        Vector3 predicted = _serverPos + _dir * (_speed * dt);
        
        // Lerp
        Vector3 newPos = Vector3.Lerp(_rigid.position, predicted, lerpFactor * Time.fixedDeltaTime);  
        
        _rigid.MovePosition(newPos);
    }

    /* ---------- 원격 MOVE 패킷 처리 ---------- */
    public void OnMovePacket(Protocol.PlayerMove mv)
    {
        // 서버 기준인 좌표, 방향, 속도 저장
        _serverPos = new Vector3(mv.Pos.X, mv.Pos.Y, mv.Pos.Z);
        
        // if(mv.PlayerId == 2)
        //     Debug.Log($"Id: {mv.PlayerId} : {_serverPos}");
        
        var dir = mv.Input.Dir;
        _dir = new Vector3(dir.X, dir.Y, dir.Z).normalized;
        _speed = mv.Input.Speed;

        _lastRecvTime = Time.time;
        
        // 즉시 스냅 (과도한 오차 보정)
        if (Vector3.Distance(transform.position, _serverPos) > snapDist)
        {
            _rigid.position = _serverPos;
            Debug.Log($"Snap: {Vector3.Distance(transform.position, _serverPos)}");
        }

    }

    // 여기부터는 Utils
    protected void SetColor(Color color)
    {
        GetComponent<Renderer>().material.color = color;
    }
}
