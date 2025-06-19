#include "pch.h"
#include "GameSession.h"
#include "GameSessionManager.h"
#include "ClientPacketHandler.h"
#include "Room.h"


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
}

void GameSession::OnSend(int32 len)
{
	STATS_PACKET_SENT();
	// Echo
	// cout << "OnSend Len = " << len << endl;
}