@echo off
echo 🧹 client_stats JSON 파일들 정리 중...

:: LoadTest 폴더 (현재는 JSON 파일 생성 안됨)
echo 📁 LoadTest 폴더 정리...
for %%f in (..\client_stats_*.json) do (
    echo 삭제: %%f
    del "%%f"
)

:: PerformanceTest 폴더 (실제 JSON 파일 생성 위치)
echo 📁 PerformanceTest 폴더 정리...
for %%f in (..\..\PerformanceTest\client_stats_*.json) do (
    echo 삭제: %%f
    del "%%f"
)

echo ✅ 정리 완료!
pause 