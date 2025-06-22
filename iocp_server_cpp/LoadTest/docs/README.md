# 🚀 부하테스트 + Prometheus + Grafana 모니터링

## 📋 개요
IOCP 서버의 실시간 부하테스트를 위한 Prometheus + Grafana 기반 모니터링 시스템입니다.

## 🎯 테스트 시나리오
- **Move 패킷**: 0.25초마다 전송 (4 TPS per client)
- **Chat 패킷**: 2초마다 전송 (0.5 TPS per client)  
- **클라이언트 수**: 1개부터 점진적 증가 (1, 3, 5, 10, 20, 50...)
- **지속시간**: 각 단계별 60초 측정

## 🛠️ 시스템 아키텍처

```
GameServer (C++)     ┌→ Prometheus (:9090) ┌→ Grafana (:3000)
├─ /metrics (:8080)  │                     │
├─ Game Logic        │                     │
└─ Performance Stats └──────────────────────┘
                     │
DummyClientCS (C#)   │
├─ /metrics (:8081+) │
├─ Move: 0.25s       │
├─ Chat: 2.0s        │
└─ Connection Pool   │
                     │
Node Exporter        │
└─ System Metrics ───┘
```

## 🚀 Quick Start

### **1. 전체 환경 구성 (Docker Compose)**
```bash
# LoadTest 디렉토리에서
docker-compose up -d
```

### **2. 부하테스트 실행**
```bash
# 점진적 부하테스트 시작
.\gradual_load_test.bat

# 또는 특정 클라이언트 수로 테스트
.\load_test.exe --clients=10 --duration=300
```

### **3. 모니터링 접속**
- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **서버 메트릭**: http://localhost:8080/metrics

## 📊 모니터링 지표

### **서버 지표 (GameServer)**
```
# HELP server_connected_clients 현재 연결된 클라이언트 수
# TYPE server_connected_clients gauge
server_connected_clients 5

# HELP server_packets_received_total 수신된 총 패킷 수
# TYPE server_packets_received_total counter  
server_packets_received_total{type="move"} 1234
server_packets_received_total{type="chat"} 567

# HELP server_processing_time_seconds 패킷 처리 시간
# TYPE server_processing_time_seconds histogram
server_processing_time_seconds_bucket{le="0.001"} 100
server_processing_time_seconds_bucket{le="0.005"} 950
server_processing_time_seconds_bucket{le="0.01"} 1000

# HELP server_memory_usage_bytes 메모리 사용량
# TYPE server_memory_usage_bytes gauge
server_memory_usage_bytes{type="heap"} 1048576
server_memory_usage_bytes{type="pool"} 2097152
```

### **클라이언트 지표 (DummyClientCS)**  
```
# HELP client_packets_sent_total 전송된 총 패킷 수
# TYPE client_packets_sent_total counter
client_packets_sent_total{client_id="1",type="move"} 234
client_packets_sent_total{client_id="1",type="chat"} 56

# HELP client_latency_seconds 응답 지연시간
# TYPE client_latency_seconds histogram
client_latency_seconds_bucket{client_id="1",le="0.01"} 100
client_latency_seconds_bucket{client_id="1",le="0.05"} 950

# HELP client_connection_status 연결 상태
# TYPE client_connection_status gauge
client_connection_status{client_id="1"} 1
```

## 🔧 상세 구현 가이드

### **Phase 1: Prometheus HTTP Server 추가**
- GameServer에 간단한 HTTP 서버 구현 (:8080/metrics)
- DummyClientCS에 HTTP 서버 추가 (:8081+/metrics)

### **Phase 2: 메트릭 수집 클래스**
- PrometheusMetrics.h/cpp (C++)
- PrometheusExporter.cs (C#)

### **Phase 3: Docker 환경 구성**
- Prometheus 설정 (prometheus.yml)
- Grafana 대시보드 (dashboard.json)  
- Node Exporter (시스템 지표)

### **Phase 4: 점진적 부하테스트**
- 클라이언트 수 자동 증가 스크립트
- 실시간 지표 수집 및 분석
- 성능 임계점 탐지

## 📈 예상 결과

### **성능 벤치마크 목표**
| 클라이언트 | 예상 TPS | 평균 지연시간 | 메모리 사용량 |
|-----------|----------|-------------|-------------|
| 1개       | 4.5      | < 5ms       | < 100MB     |
| 10개      | 45       | < 10ms      | < 200MB     |
| 50개      | 225      | < 20ms      | < 500MB     |
| 100개     | 450      | < 50ms      | < 1GB       |

### **성능 병목 지점**
- **CPU**: 패킷 처리 스레드 포화
- **Memory**: SendBuffer Pool 부족  
- **Network**: IOCP 큐 포화
- **Database**: Connection Pool 한계

## 🎯 실행 예제

```bash
# 1단계: 환경 시작
docker-compose up -d

# 2단계: 1개 클라이언트로 시작
LoadTest.exe --clients 1 --move-interval 250 --chat-interval 2000 --duration 60

# 3단계: Grafana에서 실시간 모니터링
# http://localhost:3000/d/gameserver/iocp-gameserver-dashboard

# 4단계: 클라이언트 수 점진적 증가
LoadTest.exe --progressive --max-clients 100 --step-duration 60
```

## 🎉 **완성! 실행 가능한 시스템**

### **⚡ 빠른 시작 (추천)**

```bash
# LoadTest 디렉토리로 이동
cd iocp_server_cpp/LoadTest/

# 30초 빠른 테스트 실행
.\quick_test.bat
```

### **🚀 전체 점진적 부하테스트**

```bash
# 1, 3, 5, 10, 20 클라이언트로 점진적 증가
.\gradual_load_test.bat
```

### **📊 실시간 모니터링 확인**

테스트 실행 후 브라우저에서:
- **Grafana**: http://localhost:3000 (admin/admin)
- **Prometheus**: http://localhost:9090
- **서버 메트릭**: http://localhost:8080/metrics

### **🔧 수동 실행**

```bash
# 1. Docker 환경 시작
docker-compose up -d

# 2. GameServer 수동 실행
cd ..\Binary\Release\
GameServer.exe

# 3. 클라이언트 수동 실행 (다른 터미널에서)
cd ..\DummyClientCS\bin\Release\net9.0\
DummyClientCS.exe --client-id=1 --http-port=8081 --move-interval=250 --chat-interval=2000
```

### **✅ 완성된 기능들**

- **✅ C++ GameServer**: PrometheusMetrics HTTP 서버 (:8080/metrics)
- **✅ C# DummyClientCS**: PrometheusExporter HTTP 서버 (:8081+/metrics)  
- **✅ Docker 환경**: Prometheus + Grafana + Node Exporter
- **✅ 실시간 대시보드**: 모든 주요 지표 시각화
- **✅ 자동화 스크립트**: 원클릭 실행 및 정리
- **✅ 점진적 부하테스트**: 1→3→5→10→20 클라이언트 자동 증가

이제 **실시간으로 IOCP 서버의 성능 한계를 시각적으로 확인**할 수 있습니다! 