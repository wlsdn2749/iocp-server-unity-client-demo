#include "pch.h"
#include <iostream>
#include <psapi.h> // 메모리 측정을 위해
#pragma comment(lib, "psapi.lib")

#include "SendBuffer.h"
#include "ThreadManager.h"
#include "Service.h"
#include "GameSession.h"
#include "GameSessionManager.h"
#include "BufferWriter.h"
#include "ClientPacketHandler.h"
#include "tchar.h"
#include "Protocol.pb.h"
#include "Job.h"
#include "Room.h"
#include "Player.h"
#include "DBConnectionPool.h"
#include "DBBind.h"
#include "DBSynchronizer.h"
#include "Genprocedures.h"
#include "XMLParser.h"
#include "PerformanceStats.h"
#include "PrometheusMetrics.h"

// Prometheus 메트릭 전역 객체
PrometheusMetrics* GPrometheusMetrics = nullptr;

// 패킷 직렬화 (Serialization)

//class Player
//{r
//public:
//	int32 hp = 0;
//	int32 attack = 0;
//	Player* target = nullptr;
//	vector<int32> buffs;
//};

enum
{
	WORKER_TICK = 64
};

// Ctrl+C 핸들러 (성능 통계 저장을 위해)
BOOL WINAPI ConsoleHandler(DWORD signal) {
	if (signal == CTRL_C_EVENT) {
		cout << "\n🛑 서버 종료 중... 성능 통계 저장" << endl;
		PerformanceStats::Instance()->Stop();
		cout << "✅ 성능 통계 저장 완료 (server_stats.json)" << endl;
		
		// 개별 실행 시 통계 파일 삭제 (GTest 환경이 아닌 경우)
		if (std::remove("server_stats.json") == 0) {
			cout << "🗑️ server_stats.json 파일 삭제됨 (개별 실행)" << endl;
		}
		
		ExitProcess(0);
	}
	return TRUE;
}
void DoWorkerJob(ServerServiceRef& service)
{
	static uint64 lastMemUpdateTick = 0; // 메모리 측정 주기 관리
	while (true)
	{
		LEndTickCount = ::GetTickCount64() + WORKER_TICK;

		// 네트워크 입출력 처리 -> 인게임 로직까지 (패킷 핸들러)
		service->GetIocpCore()->Dispatch(10);

		// 예약된 일감 처리
		ThreadManager::DistributeReservedJobs();

		// 글로벌 큐 (게임에서 처리되는 일감들을 처리함)
		ThreadManager::DoGlobalQueueWork();

		// Prometheus 메트릭 업데이트
		if (GPrometheusMetrics)
		{
			GPrometheusMetrics->UpdateTps();

			// 1초마다 메모리 사용량 측정
			uint64 now = ::GetTickCount64();
			if (now - lastMemUpdateTick >= 1000)
			{
				PROCESS_MEMORY_COUNTERS_EX pmc;
				if (GetProcessMemoryInfo(GetCurrentProcess(), reinterpret_cast<PROCESS_MEMORY_COUNTERS*>(&pmc), sizeof(pmc)))
				{
					// pmc.WorkingSetSize : 실제 물리 메모리 사용량
					// pmc.PrivateUsage   : 커밋된 가상 메모리(Private Bytes)
					GPrometheusMetrics->UpdateMemoryUsage(static_cast<int64>(pmc.PrivateUsage), 0); // 풀 메모리는 0으로 표기
				}
				lastMemUpdateTick = now;
			}
		}
	}
}

int main()
{
	ASSERT_CRASH(GDBConnectionPool->Connect(10, L"Driver={ODBC Driver 17 for SQL Server};Server=(localdb)\\MSSQLLocalDB;Database=ServerDb;Trusted_Connection=Yes;"));

	DBConnection* dbConn = GDBConnectionPool->Pop();
	DBSynchronizer dbSync(*dbConn);
	dbSync.Synchronize(L"GameDB.xml");

	{
		WCHAR name[] = L"홍길동";
		SP::InsertGold insertGold(*dbConn);
		insertGold.ParamIn_Gold(100);
		insertGold.ParamIn_Name(name);
		insertGold.ParamIn_CreateDate(TIMESTAMP_STRUCT{2020, 10, 19});
		insertGold.Execute();
	}

	{
		SP::GetGold getGold(*dbConn);
		getGold.ParamIn_Gold(100);

		int32 id = 0;
		int32 gold = 0;
		WCHAR name[100];
		TIMESTAMP_STRUCT date;

		getGold.ColumnOut_Id(OUT id);
		getGold.ColumnOut_Gold(OUT gold);
		getGold.ColumnOut_Name(OUT name);
		getGold.ColumnOut_CreateDate(OUT date);

		getGold.Execute();

		while (getGold.Fetch())
		{
			GConsoleLogger->WriteStdOut(Color::BLUE,
				L"ID[%d] Gold[%d] Name[%ls]\n", id, gold, name);
		}

	}

	ClientPacketHandler::init();

	// Ctrl+C 핸들러 등록 (성능 통계 저장을 위해)
	if (!SetConsoleCtrlHandler(ConsoleHandler, TRUE)) {
		cout << "⚠️ 시그널 핸들러 등록 실패" << endl;
	}

	// 성능 통계 수집 시작
	PerformanceStats::Instance()->StartPeriodicSave(1); // 1초마다 통계 저장
	cout << "🚀 GameServer started with performance monitoring" << endl;

	// Prometheus 메트릭 시작
	GPrometheusMetrics = new PrometheusMetrics();
	if (GPrometheusMetrics->Start()) {  // 기본 포트 10001 사용
		cout << "📊 Prometheus 메트릭 서버 시작됨 (포트 10001)" << endl;
	} else {
		cout << "❌ Prometheus 메트릭 서버 시작 실패" << endl;
	}

	ServerServiceRef service = MakeShared<ServerService>(
		NetAddress(L"127.0.0.1", 8421),
		MakeShared<IocpCore>(),
		MakeShared<GameSession>, // TODO,  ()가 필요없는건, CreateSession으로 넘어가서 실행하기 때문
		100);

	ASSERT_CRASH(service->Start());
	for (int32 i = 0; i < 5; i++)
	{
		GThreadManager->Launch([&service]()
		{
			while (true)
			{
				// Worker Thread가 Queue에서 Event를 꺼냄
				//service->GetIocpCore()->Dispatch();
				DoWorkerJob(service);

			}
		});
	}

	GThreadManager->Join();
}
