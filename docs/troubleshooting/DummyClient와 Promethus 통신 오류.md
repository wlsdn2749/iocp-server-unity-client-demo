---
tags: [troubleshooting]
date: 2025-07-04
---
# DummyClient와 Promethus 통신 오류

## **1. 환경**
- 커밋 :  
- 버전 :  

## **2. 증상**

Server를 실행해 Promethus와의 연결은 성공했으나, 각 DummyClient와의 연결은 실패하였음. 

## **3. 원인**
URL ACL이 정책이라, 이를 등록해주어야 한다고 함.

## **4. 해결**
```shell
@echo off
echo 📡 Prometheus용 MetricServer 포트 등록 시작 (10100~10200)
echo 관리자 권한으로 실행되어야 합니다.

for /L %%i in (10100,1,10200) do (
  echo 등록 중: 포트 %%i
  netsh http add urlacl url=http://+:%%i/metrics user=Everyone >nul 2>&1
  if %ERRORLEVEL% EQU 0 (
    echo ✅ 성공: http://+:%%i/metrics
  ) else (
    echo ⚠️ 이미 등록되었거나 실패: http://+:%%i/metrics
  )
)

echo 🟢 완료되었습니다.
pause
```
