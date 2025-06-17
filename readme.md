<table>
  <tr>
    <td><img src="/statics/demo.gif" width="800" height="450"></td>
  </tr>
</table>

<div align="center">

**C++ IOCP Server and C# Unity Client DEMO**

______________________________________________________________________

</div>


## Introduction

본 프로젝트는 [Rookiss님의 Inflearn 강의 시리즈]인  
- [C++과 언리얼로 만드는 MMORPG 게임 개발 시리즈 Part4: 게임 서버](https://www.inflearn.com/course/%EC%96%B8%EB%A6%AC%EC%96%BC-3d-mmorpg-4/dashboard)  
- [C#과 유니티로 만드는 MMORPG 게임 개발 시리즈 Part4: 게임 서버](https://www.inflearn.com/course/%EC%9C%A0%EB%8B%88%ED%8B%B0-mmorpg-%EA%B0%9C%EB%B0%9C-part4)

를 기반으로 구성되었습니다.

- **서버**는 C++ 기반 IOCP 모델을 그대로 유지하고 있으며,  
- **클라이언트**는 Unity(C#)에서 동작하지만, 원래 구조 대신 **Google Protobuf 기반 통신 방식으로 개조**하여 **서버와 완전한 호환이 가능하도록 수정**한 구조입니다.

양측은 동일한 `.proto` 파일을 공유하며, 패킷 구조는 자동화 도구를 통해 유지·관리됩니다.
이 프로젝트는 기존 강의에서 제공되는 학습 기반 코드를 실전 수준으로 구조화하고 확장하는 데 목적이 있습니다.

## Features
- 비동기 IOCP 소켓 + Protobuf 프로토콜 버퍼
- 패킷, 프로시저 코드 자동화
- MemoryPool, ObjectPool등 여러 풀링 기술
- 실시간 위치 동기화 데모 (.gif 참조)


## Quick Start
- Visual Studio Server측 빌드 -> `Binary/Releaese/GameServer.exe` 실행
- Unity 빌드 후, Unity `Client.exe` 실행



## PacketGenerator

Protobuf `.proto` 파일을 기반으로 C++/C# 패킷 코드(CS/H)를 자동 생성하는 도구입니다.

---

> ⚠ 반드시 `Common/Protobuf/bin` 폴더 안에서 실행하세요.  
> PacketGenerator 내용을 수정한 후, MakeExe로 GenPackets.exe를 만들고 
> `Templates/` 폴더와 실행 파일 (`GenPacket.exe`)를 복사해서 `Common/Protobuf/bin/`부분에 넣어 사용하면된다.


> 또한 `Common/Protobuf/bin` 부분에서 .proto 파일을 컨텐츠 코드에 맞게 수정할 수 있겠다.



---
## 🧩 Dependencies

### Server (C++)
- Windows 환경 / Visual Studio 2022
- WinSock / IOCP 관련 API
- Protobuf C++ 라이브러리 3.21.12

### Client (C# Unity)
- Unity 6000.0.31f1(빌드시 백그라운드 프로세스 버그 있음) -> **Unity 6000.1.7f1**
- `Google.Protobuf` .dll 실행파일 필요
- 실행파일 4가지, 모두 다운받아 Assets/Plugins 에 넣었음
    - https://www.nuget.org/packages/Google.Protobuf/3.21.12
    - https://www.nuget.org/packages/System.Memory/4.6.3
    - https://www.nuget.org/packages/System.Runtime.CompilerServices.Unsafe/6.1.2
    - https://www.nuget.org/packages/System.Buffers/4.6.1

### PacketGenerator (Python / jinja2)
- Python: 3.10
- Jinja2: 3.1.6
- Pyinstaller를 통한 실행파일 생성

<!-- ## ✅ 개발 Todo -->


<!-- - [ ] 🎮 **채팅 기능 시연**  
      Player Count 표시 + YouTube 시연 영상 추가 + DB Insert 확인
- [ ] 🗃️ **DB 연동 마무리**  
      프로시저 자동 생성 기능 완료
---
- [ ] **C# DummyClient 구현**
- [ ] **SendBuffer 풀링 최적화 + Protobuf 직렬화 지원**
- [ ] **클라이언트 Disconnect 처리 시 세션 안전 제거**
--- -->

## Development, testing, and QA checklist

- [ ] **C# DummyClient 구현**
- [X] **채팅 기능 구현 // 현재 룸에 접속중인 클라이언트 확인 기능 추가**
- [ ] **DummyClient – 로그인 → 이동 100회 → 채팅 10회 → 종료**
- [ ] 위의 시나리오의 성능 측정

<!-- ### Unit Test (GoogleTest, Unity Test Framework)
- [ ] `MemoryPool` 테스트 – 재할당 후 패턴 유지 확인
- [ ] `RecvBuffer` / `SendBuffer` – 경계값(0, 1, BUFFER_SIZE±1) 테스트
- [ ] `PacketSession::OnRecv` – 조각화 패킷 처리 (`BytesProcessed` 검증)
- [ ] `DBConnectionPool` – Mock Handle로 호출 횟수 검증 -->

### Integration & End-to-End



<!-- ### Performance & Stability
- [ ] wrk-tcp / custom cpp client를 통한 TPS 측정
- [ ] Unity Ping/Pong RTT 및 Jitter 그래프 확인
- [ ] 메모리 릭 검사 (서버: CRTDBG, 클라: Unity Profiler) -->
