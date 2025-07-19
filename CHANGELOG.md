# Changelog

All notable changes to this project will be documented in this file.

## [0.0.31] - 2025-07-17

### 🚀 Features

- *(Chat)* Receive-Broadcast Style to Tick Broadcast Style updated it makes good efficiency and takes low TPS so server has more availability

## [0.0.29] - 2025-07-17

### ⚙️ Miscellaneous Tasks

- *(Test1)* For Test, deactivate Chat broadcast, after few days, logic will be changed

## [0.0.28] - 2025-07-09

### 🐛 Bug Fixes

- *(Move)* 225명쯤 동시접속에서, 분할했음에도 불구하고 Move 패킷에서 Nullptr 오류가 났었음

## [0.0.27] - 2025-07-09

### 🚜 Refactor

- *(Move)* Room에 100명 이상 동시접속하게 되면, BroadCasting 할때의 총 패킷이 SendBufferChunkSize의 크기를 넘어버린다. 그래서 패킷을 초과할 여지가 있는 브로드 캐스팅의 경우, 길이를 미리 계산해서 분할송신한다.

## [0.0.26] - 2025-07-09

### 🐛 Bug Fixes

- *(Move)* 100명 가까이 이동하면, 모든 더미클라이언트가 뒤로 밀리는 현상이 발생했음. 보험으로 TCP 패킷 순서를 체크해서, 지연으로 인해 나중에 도착하는 패킷이 있다면? (TCP인데 그럴 수가 있나..?) 아무튼 그러면 드랍하도록 변경합.

## [0.0.25] - 2025-07-09

### 🚜 Refactor

- *(Move)* 지연시간으로 인해 클라이언트와 서버의 좌표오차가 발생해, 서버에서 비정상적으로 움직이는 부분을 수정했다. 하지만 러버밴딩문제가 발생해 50~100명 클라이언트가 동시 접속시, 앞뒤로 순간이동하다 다시 돌아오는 문제가 발생해 이 부분을 수정해야한다.

## [0.0.24] - 2025-07-08

### 🚜 Refactor

- *(Move)* 이동 로직을 수정했다. 기존에 A좌표에서 B좌표로 가는걸 Client가 Simulation 했다면, 지금은 서버에서 이동을 처리하고 방향과 위치를 넘겨주어 클라이언트에서 Lerp로 자연스럽게 가도록 변경했다. 이전의 코드에서는 클라이언트를 실행할때마다 초기위치가 완전 랜덤이라 수정이 필요했음

## [0.0.23] - 2025-07-07

### ⚙️ Miscellaneous Tasks

- UI, Readme update

## [0.0.22] - 2025-07-04

### 📚 Documentation

- Readme.md 수정 및 트 슈팅 파일 Docs/에 정리

## [0.0.21] - 2025-07-03

### 🐛 Bug Fixes

- *(Load)* 100명 이상 부하테스트 추가 및 동시접속자 100명 이상 끊김 현상 해결

## [0.0.20] - 2025-07-01

### 🐛 Bug Fixes

- *(move)* 이동 패킷의 뒤로 끊김 현상은, 단순히 브로드캐스팅 하는 과정에서, 자기가 보낸 패킷을 갱신해서 그런 것임

## [0.0.19] - 2025-07-01

### 🐛 Bug Fixes

- *(chat)* 채팅 패킷을 주고 받는과정에서 끊기는것을 Profiler로 확인, 그리고 로직을 수정하여 해결함

## [0.0.18] - 2025-06-30

### 🚀 Features

- *(DB-Register-UnityClient)* 유니티 클라이언트 Register UI 추가, Register부분의 예외처리가 지금 DB Flow에 의해 조금 방해되는 상태임. 이를 수정해야함

## [0.0.16] - 2025-06-27

### 🚀 Features

- *(DB)* DB feature Login added, for that, DBBind's Some overload function changed and automation code changned

## [0.0.15] - 2025-06-26

### 🚀 Features

- *(DB)* DB feature Register added

### 🚜 Refactor

- DBBind and DBConnection automation don't support output parameters before, so fix it and refactor it

### 📚 Documentation

- Update readme.md MS-SQL supported

## [0.0.14] - 2025-06-24

### 🚀 Features

- *(DB)* MS-SQL DB Beta Supported

### 🐛 Bug Fixes

- In Will be added ConsoleLogger's Output, Korean output has been broken out. fixed it

## [0.0.13] - 2025-06-22

### ⚙️ Miscellaneous Tasks

- Prometheus + Grafana LoadTest commited, it is used to check Server RAM, PPS, client RTT, and watch visualization

## [0.0.12] - 2025-06-19

### 📚 Documentation

- Update readme.md packetGernerator mermaid chard

## [0.0.11] - 2025-06-19

### 📚 Documentation

- Update readme.md based on test automation

### ⚙️ Miscellaneous Tasks

- Automation Optimization, Now just run PerformanceTest/network_test.bat, you will make automation test and getting result based on real network communications

## [0.0.10] - 2025-06-19

### ⚙️ Miscellaneous Tasks

- Test automation using batch file completed in local status

## [0.0.9] - 2025-06-19

### ⚙️ Miscellaneous Tasks

- Local test automation maked but it is yet unstable...

## [0.0.8] - 2025-06-18

### 🚀 Features

- *(C# DummyClient)* You can use from now, both of C# and C++ DummyClient. They seems like working well

### ⚙️ Miscellaneous Tasks

- Remove TODO comment in Automation code. It is already implemented so that the code is useless

## [0.0.7] - 2025-06-17

### 📚 Documentation

- Update readme.md

## [0.0.6] - 2025-06-17

### 🚀 Features

- *(room count)* Update interval 0.5s, client can see how many peoples in this roomon top-left side TMP. that count is local variable unity client handle an array contains players. that array is changed by other client disconnected. but in case of C++ DummyClient even if disconnecting doesn't work well. it only works well in unity client and builts.

## [0.0.5] - 2025-06-17

### 🚀 Features

- *(chat)* Chat broadcasting

## [0.0.4] - 2025-06-17

### 🐛 Bug Fixes

- Server's ClientPacketHandler.css, in case of using Room's function e.g) broadcast or enter... have to use DoAsync() instead of GRoom->... because JobQueue is built by processing one thread only. if using GRoom->... in outside of Room.cpp may cause data races so fixed it and Chatiing demo revised

## [0.0.3] - 2025-06-16

### 📚 Documentation

- Update readme.md

## [0.0.2] - 2025-06-16

### 📚 Documentation

- Update readme.md

### ⚙️ Miscellaneous Tasks

- *(Template)* Remove comments
- Github action added

## [0.0.1] - 2025-06-14

### 💼 Other

- Demo Client Server Communication and not yet completing automation
- Client side code to be ready automation
- .cs file automation completed.
- SendBuffer Function with automation. and Brief Readme
- Room based broadcast and two error fixed
- Server to client Broadcasting and dummy client session management code added
- Demo

<!-- generated by git-cliff -->
