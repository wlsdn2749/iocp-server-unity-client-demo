#include "pch.h"
#include "GameSession.h"
#include "GameSessionManager.h"
#include "ClientPacketHandler.h"
#include "Room.h"
#include "PrometheusMetrics.h"

// Prometheus 메트릭 (외부 선언)
extern PrometheusMetrics* GPrometheusMetrics;

void GameSession::OnConnected()
{
	GSessionManager.Add(static_pointer_cast<GameSession>(shared_from_this()));
	STATS_CLIENT_CONNECTED();
}

void GameSession::OnDisconnected()
{
	GSessionManager.Remove(static_pointer_cast<GameSession>(shared_from_this()));
	STATS_CLIENT_DISCONNECTED();

	if (_currentPlayer)
	{
		if (auto room = _room.lock())
			room->DoAsync(&Room::Leave, _currentPlayer);
	}

	_currentPlayer = nullptr;
	_players.clear();
}

// 완전히 조립된 패킷이라고 판단
void GameSession::OnRecvPacket(BYTE* buffer, int32 len)
{
	auto start = std::chrono::high_resolution_clock::now();
	
	PacketSessionRef session = GetPacketSessionRef();
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);

	// TODO : packetId 대역 체크
	ClientPacketHandler::HandlePacket(session, buffer, len);
	
	auto end = std::chrono::high_resolution_clock::now();
	auto duration = std::chrono::duration_cast<std::chrono::microseconds>(end - start);
	
	STATS_PACKET_RECEIVED();
	STATS_RECORD_PROCESSING_TIME(duration.count() / 1000.0); // microseconds to milliseconds
	
	// Prometheus 메트릭 업데이트 (패킷 처리 시간)
	double processingTimeMs = duration.count() / 1000.0;
	if (GPrometheusMetrics) {
		GPrometheusMetrics->UpdateProcessingTime(processingTimeMs);
		
		// 디버깅용 로그 (처음 몇 개만)
		static int logCount = 0;
		if (logCount < 5) {
			std::cout << "[DEBUG] 패킷 처리 시간: " << processingTimeMs << "ms" << std::endl;
			logCount++;
		}
	}
}

void GameSession::OnSend(int32 len)
{
	STATS_PACKET_SENT();
	// Echo
	// cout << "OnSend Len = " << len << endl;
}	