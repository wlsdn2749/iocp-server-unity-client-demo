---
tags: [troubleshooting]
date: 2025-07-04
---
# 여러 클라이언트를 실행시 DB와 통신오류

## **1. 환경**
- 커밋 :  Issue #12
- 버전 :  

## **2. 증상**
한번에 여러 클라이언트를 실행할 경우, (2개든 30개든)
DB 로직이 뭔가 잘못된 경우, (예외를 일으키는 경우) Unbind 부분에서 에러가 잡힌다.

1개를 실행하는경우 예외일으키는 경우 예외핸들링에서 잡힌다.

예를들면
1번 클라이언트가 Login을 시도하는 부분에서, 실패한 경우 (예외 로직 실행)
여기서 같은 Id, 같은 pw로 똑같이 Login을 시도하면 예외로직이 실행되지 않고 바로 크래시가 나버린다.
\_statement 가 nullptr


## **3. 원인**
Server측의 DBConnectionPool의 최대 접근 가능 제한이 1이였음

- 핵심 코드 :
```cpp
ASSERT_CRASH(GDBConnectionPool->Connect(10, L"Driver={ODBC Driver 17 fo...")
// 문제 지점 하이라이트
// 저 10부분이 원래는 1이였음
```

- 발생 이유 요약 : DBConnectionPool을 하나만 사용하게 되면 다른 ConnectionPool을 사용할 수 없어 nullptr아 뜨는 것이였음.

## **4. 해결**

저 부분을 1 -> 10으로 늘려줌

## **5. 회고**

- 놓친 이유 :
- 예방 조치 : 나중에는 연결할 때, 자동으로 DBConnectionPool에서 Push 하고 사용하면 반납하는 그런 구조를 만들어야겠음