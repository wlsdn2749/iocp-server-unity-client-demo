<table>
  <tr>
    <td><img src="/statics/demo.gif" width="1152" height="648"></td>
  </tr>
</table>

<div align="center">

**C++ IOCP Server and C# Unity Client DEMO**

______________________________________________________________________

</div>


## 📖 Introduction

본 프로젝트는 [Rookiss님의 Inflearn 강의 시리즈]인  
- [C++과 언리얼로 만드는 MMORPG 게임 개발 시리즈 Part4: 게임 서버](https://www.inflearn.com/course/%EC%96%B8%EB%A6%AC%EC%96%BC-3d-mmorpg-4/dashboard)  
- [C#과 유니티로 만드는 MMORPG 게임 개발 시리즈 Part4: 게임 서버](https://www.inflearn.com/course/%EC%9C%A0%EB%8B%88%ED%8B%B0-mmorpg-%EA%B0%9C%EB%B0%9C-part4)

를 기반으로 구성되었습니다.

- **서버**는 C++ 기반 IOCP 모델을 그대로 유지하고 있으며,  
- **클라이언트**는 Unity(C#)에서 동작하지만, 원래 구조 대신 **Google Protobuf 기반 통신 방식으로 개조**하여 **서버와 완전한 호환이 가능하도록 수정**한 구조입니다.

양측은 동일한 `.proto` 파일을 공유하며, 패킷 구조는 자동화 도구를 통해 유지·관리됩니다.

이 프로젝트는 기존 강의에서 제공되는 학습 기반 코드를 실전 수준으로 구조화하고 확장하는 데 목적이 있습니다.

- 송수신 패킷 통신 O
- 브로드캐스팅 O (데모 O)


## PacketGenerator

Protobuf `.proto` 파일을 기반으로 C++/C# 패킷 코드(CS/H)를 자동 생성하는 도구입니다.

---

### 📌 사용법

> ⚠ 반드시 `Common/Protobuf/bin` 폴더 안에서 실행하세요.  
> PacketGenerator 내용을 수정한 후, MakeExe로 GenPackets.exe를 만들고 
> `Templates/` 폴더와 실행 파일 (`GenPacket.exe`)를 복사해서 `Common/Protobuf/bin/`부분에 넣어 사용하면된다.


> 또한 `Common/Protobuf/bin` 부분에서 .proto 파일을 컨텐츠 코드에 맞게 수정할 수 있겠다.



---
## 🧩 의존성

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

### TODO
- Client:
  - C# 버전 DummyClient 제작
  - SendBuffer에서의 Pooling 기능 추가 & Protobuf 사용
  
- Server:
  - Client Disconnection시 Session 삭제