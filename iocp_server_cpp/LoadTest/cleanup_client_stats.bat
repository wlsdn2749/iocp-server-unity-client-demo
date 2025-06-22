@echo off
echo 🧹 client_stats JSON 파일들 정리 중...

for %%f in (client_stats_*.json) do (
    echo 삭제: %%f
    del "%%f"
)

echo ✅ 정리 완료!
pause 