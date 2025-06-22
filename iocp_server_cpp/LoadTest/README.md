# 🚀 LoadTest - 부하테스트 + 모니터링 시스템

## 📁 **폴더 구조**

```
LoadTest/
├── 📂 scripts/           # 실행 스크립트들
│   ├── cleanup_client_stats.bat      # JSON 파일 정리
│   ├── gradual_load_test.bat         # 점진적 부하테스트  
│   ├── quick_test.bat                # 빠른 테스트
│   ├── register_acl.bat              # ACL 등록
│   ├── restart_monitoring.bat        # 모니터링 재시작
│   └── simple_test.bat               # 간단한 테스트
│
├── 📂 configs/          # 설정 파일들
│   ├── docker-compose.yml           # Docker 환경 설정
│   ├── nginx.conf                   # Nginx 프록시 설정
│   └── prometheus.yml               # Prometheus 설정
│
├── 📂 docs/             # 문서들
│   ├── performance_guide.md         # 성능 가이드
│   └── README.md                    # 상세 매뉴얼
│
├── 📂 grafana/          # Grafana 설정
│   ├── dashboards/                  # 대시보드 정의
│   └── provisioning/                # 자동 프로비저닝
│
└── README.md            # 이 파일 (간단 가이드)
```

## 🚀 **빠른 시작**

### **1. 즉시 테스트 (30초)**
```bash
cd iocp_server_cpp/LoadTest/
.\scripts\quick_test.bat
```

### **2. 점진적 부하테스트 (5분)**
```bash
.\scripts\gradual_load_test.bat
```

### **3. 모니터링 확인**
- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **서버 메트릭**: http://localhost:8080/metrics

## 📊 **핵심 기능**

### ✅ **실시간 메트릭 수집**
- **서버**: 연결 수, PPS, 패킷 처리 시간, 메모리 사용량
- **클라이언트**: 지연시간, 전송 패킷 수, 연결 상태
- **시스템**: CPU, 메모리, 네트워크 I/O

### ✅ **자동화된 테스트**
- **점진적 증가**: 1→3→5→10→20→50 클라이언트
- **패킷 타입**: Move(0.25초), Chat(2초), RTT(5초)
- **성능 통계**: JSON 파일 자동 생성 (PerformanceTest에서만)

### ✅ **시각화 대시보드**
- **실시간 그래프**: 시간대별 성능 추이
- **메트릭 비교**: 서버 vs 클라이언트 지표
- **알림 시스템**: 임계값 초과 시 경고

## 🛠️ **스크립트 설명**

| 스크립트 | 설명 | 용도 |
|---------|------|------|
| `quick_test.bat` | 5클라이언트 30초 테스트 | 빠른 동작 확인 |
| `gradual_load_test.bat` | 점진적 부하테스트 | 성능 한계 탐지 |
| `simple_test.bat` | 기본 테스트 | 디버깅용 |
| `restart_monitoring.bat` | 모니터링 재시작 | 설정 변경 후 |
| `cleanup_client_stats.bat` | JSON 파일 정리 | 디스크 공간 확보 |

## 🎯 **성능 목표**

| 클라이언트 | 예상 TPS | 평균 지연시간 | 메모리 |
|-----------|----------|-------------|--------|
| 1개       | 4.5      | < 5ms       | 100MB  |
| 10개      | 45       | < 10ms      | 200MB  |
| 50개      | 225      | < 20ms      | 500MB  |
| 100개     | 450      | < 50ms      | 1GB    |

## 🔧 **수동 실행**

```bash
# 1. Docker 환경 시작
docker-compose -f configs/docker-compose.yml up -d

# 2. GameServer 실행
cd ..\Binary\Release\
GameServer.exe

# 3. 클라이언트 실행
cd ..\DummyClientCS\bin\Release\net9.0\
DummyClientCS.exe --clients=5 --duration=60
```

## 📈 **결과 분석**

### **성능 지표**
- **PPS (Packets Per Second)**: 실시간 패킷 처리율
- **지연시간**: 클라이언트 → 서버 → 클라이언트 RTT
- **처리시간**: 서버 내부 패킷 처리 시간
- **메모리**: 힙/풀 메모리 사용량

### **병목 지점**
- **CPU**: 패킷 처리 스레드 포화 상태
- **메모리**: SendBuffer Pool 부족
- **네트워크**: IOCP 큐 포화
- **DB**: Connection Pool 한계

---

## 📝 **상세 문서**

더 자세한 내용은 [`docs/README.md`](docs/README.md)를 참고하세요.

**🎉 이제 실시간으로 IOCP 서버의 성능을 모니터링할 수 있습니다!** 