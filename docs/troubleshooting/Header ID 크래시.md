---
tags: [troubleshooting]
date: 2025-07-04
---
# Header ID 크래시

## 1. 환경
- 날짜/커밋 :  6c155b49d4c642ab64a142bff40cd382a22953ae
- 버전 :  v0.0.1  

## 2. 증상
Server측의 Room.cpp의 Broadcast에서 크래시 발생함

## 3. 원인
`ClientPacketHandler::MakeSendBuffer`로 만든 enterBuffer에서 

header값이 잘못 입력되어있었다.
size, id가 비정상적인 값이 나온것인데,  
size는 괜찮더라도 id가 문제였다. 이 ID를 통해 GPacketHandler를 찾게되는데,
정의되지 않은 배열을 참조하므로, Out of Range로  Abort() 크래시가 난것이다. 

- 핵심 코드 :
```cpp 
// ServerCore::Session.cpp

int32 PacketSession::OnRecv(BYTE* buffer, int32 len)
{
	int32 processLen = 0; // ① 이번 호출에서 이미 처리한 총 바이트
	while (true) 
	{
		int32 dataSize = len - processLen; // ② 아직 안 읽은 바이트 수
		// 최소한 헤더는 파싱할 수 있는지?

		if (dataSize < sizeof(PacketHeader))
			break;							// ③ 헤더(4B)조차 못 읽으면 다음 recv 때 이어서

		// ④ 현재 오프셋(processLen)에서 헤더 구조체를 읽는다
		PacketHeader header = *(reinterpret_cast<PacketHeader*>(& buffer[0])); // 여기부분 실수;;

		// ⑤ 헤더가 알려 준 ‘전체 패킷 길이’(size) 만큼 모두 도착했는가?
		if (dataSize < header.size)
			break;

		// ⑥ 한 패킷(헤더+본문)이 완성 → 상위 핸들러로 넘김
		OnRecvPacket(&buffer[processLen], header.size);

		processLen += header.size;
	}

	return processLen;  // ⑧ 이번에 소비한 바이트 수 리턴
}
// 문제 지점 하이라이트
// PacketHeader header = *(reinterpret_cast<PacketHeader*>(& buffer[0])); // 여기부분 실수;
```

헤더를 파싱하는 부분에서, offset 위치를 갱신하지 않아서 발생하는 문제였다.

- 발생 이유 요약 : **버퍼를 읽고 다음 위치로 헤더 위치를 옮겨야하는데, 옮기지 않고 그대로 0번째 위치를 읽고 있었음**

_조금 더 자세히 이 문제가 어떠한 원리로 발생하는지 알아본다..._

지금까지는 왜 문제가 발생하지 않았는가?
- 같은 문자열의 같은 ID로 테스트 했기 때문에 **(1001, "Hello world")**
  이 문자열만 계속 주고 받았기 때문에 문제가 발생하지 않았던 것이다.
- `header[0]`이나 `header[processLen]`이나 어차피 같은 헤더값을 가질 것 이기 때문에, 문제가 없었지만 BroadCast가 들어가면서 사정이 약간 달라졌다.

OnRecv의 Buffer 매개변수로 여러 ID값, Packet Size를 가진 패킷이 들어올 수 있게 바뀌었기 때문에 발생한 문제이다.

만약 다음과 같이 크기와 ID가 서로 다른 패킷이 들어왔다면
패킷 A: size 19, id 1006
패킷 B: size 30, id 1007
패킷 C: size 23, id 1005 

`PacketSession::OnRecv` 의 첫 루프는 패킷 A가 들어와도 문제가 없을 것이다. 하지만 다음 루프에서 여전히 헤더는 `Buffer[0]`을 가리키고 있을 것이고, 당연히 패킷 A인 `Buffer[0]`의 헤더값을 읽어 size를 19로 해석할 것이다. 이 상태에서 `GameSession::OnRecvPacket` 을 호출하면, 헤더 위치는 `processlen`으로 정상적인데, size가 달라지게 된다 (30 -> 19)

```cpp
void GameSession::OnRecvPacket(BYTE* buffer, int32 len)
{
	PacketSessionRef session = GetPacketSessionRef();
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);

	// TODO : packetId 대역 체크
	ClientPacketHandler::HandlePacket(session, buffer, len);
}
```
이 부분에서 buffer는 정상값, len은 비정상값이 넘어가게 될 것이다.

```cpp
static bool HandlePacket(PacketSessionRef& session, BYTE* buffer, int32 len)
{
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
	return GPacketHandler[header->id](session, buffer, len);
}
```

그러면 이부분이 호출되는데. 여기서 그래도 header값은 정상적이므로, Abort까지 뜨지는 않게된다.

다음 호출은 processLen이 이전에 잘못된 header.size를 더한것으로 인해
processLen의 현재 위치가. 패킷의 헤더 부분이 아니라 데이터 한복판에 위치하게 된다.

그러면 header는 여전히 buffer\[0]을 읽고 있기 때문에 A의 Header를 가리킬 것이고
OnRecvPacket으로 넘어가는 buffer의 경우 &buffer\[processLen]이 잘못된 위치를 가리킬 것이고 size또한 잘못된 값으로 넘어갈 것이다.
그럼

```cpp
void GameSession::OnRecvPacket(BYTE* buffer, int32 len)
{
	PacketSessionRef session = GetPacketSessionRef();
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);

	// TODO : packetId 대역 체크
	ClientPacketHandler::HandlePacket(session, buffer, len);
}
```
다시 이부분에서. buffer를 파싱하는 과정에서 잘못된 값으로 header의 id와 size가 잘못된 값이 나오게되고 
여기서 packetId의 대역을 체크했으면 괜찮은데, 체크하지 않고 넘기므로

```cpp
static bool HandlePacket(PacketSessionRef& session, BYTE* buffer, int32 len)
{
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
	return GPacketHandler[header->id](session, buffer, len);
}
```
여기까지 오게되고 여기서 header가 이상하고, 이상한 header의 id값을 보자니

2번째 문제가 발생하게 된다.

Server측의 ClientPacketHandler 코드는 다음과 같다.
```cpp
static void init()
{
	for (int32 i = 0; i < UINT16_MAX; i++)
	{
		GPacketHandler[0] = Handle_INVALID;
	}
	GPacketHandler[PKT_C_LOGIN] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::C_LOGIN>(Handle_C_LOGIN, session, buffer, len); };
	GPacketHandler[PKT_C_ENTER_GAME] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::C_ENTER_GAME>(Handle_C_ENTER_GAME, session, buffer, len); };
	GPacketHandler[PKT_C_CHAT] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::C_CHAT>(Handle_C_CHAT, session, buffer, len); };
	
}
```

이 코드는 수정되기 전 코드인데, 어디가 문제일까?
뭔가 이상하지 않은가? UINT16_MAX가 65536이므로 최대 65536배열까지 처리할 수 있음에도 불구하고 **(HANDLE_INVALID)**

id 가 65536을 넘어가지 않음에도 nullptr이 뜨게된다.
그 이유는 저기 `GPacketHandler[0] = HANDLE_INVALID;` 가 문제이다.
원래라면 `GPacketHandler[i] = HANDLE_INVALID`가 되는게 맞다. 그러모르 크래시가 낫던것.

만약 저기 부분이 i였다면 크래시는 나지않고 기능은 정상적으로 동작하지 않은채 돌아갔을 것이다.

## **5. 해결**

- 수정 커밋 : 6c155b49d4c642ab64a142bff40cd382a22953ae
- 변경 코드 :  위에 언급.
## **6. 회고**

- 놓친 이유 : ServerCore쪽은 웬만하면 건드릴 일이 없다보니, 처음 완성한 코드가 맞다고 판단하여 계속 진행했음
- 예방 조치 : assert, 코드를 꼼꼼하게 확인하기