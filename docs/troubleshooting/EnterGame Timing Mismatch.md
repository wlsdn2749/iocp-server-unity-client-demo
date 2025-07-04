---
tags: [troubleshooting]
date: 2025-07-04
---
# EnterGame Timing Mismatch

## **1. 환경**
- 커밋 :  b174715709de9e4fa07d9afde575aafc903ae1e0
- 버전 :  v0.0.7

## **2. 증상**

클라이언트가 아직 서버의 Room에 연결되기전 패킷을 보내버리는 증상이 발생함.

## **3. 원인**
- 핵심 코드 :
```cpp
  public void SendForEachMove()
        {
            if (!_canSendPackets) return; // ENTER_GAME 완료 전에는 패킷 송신 금지

            lock (_lock)
            {
                foreach (ServerSession session in _sessions)
                {
                    Protocol.C_MOVE movePacket = new Protocol.C_MOVE();
                    movePacket.PosX = _rand.Next(-50, 50);
                    movePacket.PosY = 0;
                    movePacket.PosZ = _rand.Next(-50, 50);
                    ArraySegment<byte> segment = ServerPacketManager.MakeSendBuffer(movePacket);

                    session.Send(segment);
                }
            }
        }
// 문제 지점 하이라이트
// if (!_canSendPackets) return; // ENTER_GAME 완료 전에는 패킷 송신 금지
```

Client와 Server가 연결되고
Client가 Server의 Session에 등록되기 위해서는
C_Login -> S_Login -> C_BroadCast ... 를 통해 Server측 Session에 player를 등록하는 과정을 거쳐야하는데, 그 과정 전에 SendForEach를 떄려버리기 때문에 

C_Chat과 C_Move를 Handling하는 과정에서 `gameSession->_currentPlayer`가 nullptr이 뜨게 되어 크래시가 발생한다.


- 발생 이유 요약 : 무차별적인 패킷 송신

## **4. 해결**

- 수정 커밋 : 681fab0223e833b7028fc595eed8cb5e3709a4dd
- 변경 코드 :  `if (!_canSendPackets) return; // ENTER_GAME 완료 전에는 패킷 송신 금지`
+ `Enter_Game시, Flag 변경`

## **5. 회고**

- 놓친 이유 : 패킷 순서를 명확하게 정의하지 않고 진행하였음
- 예방 조치 : Session간 상태 전이를 명확하게 정의하고 꼼꼼하게 진행 하는게 좋아보임.