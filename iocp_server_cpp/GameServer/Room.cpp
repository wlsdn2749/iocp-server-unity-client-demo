#include "pch.h"
#include "Room.h"

#include "ClientPacketHandler.h"
#include "Player.h"
#include "GameSession.h"
#include "Protocol.pb.h"

shared_ptr<Room> GRoom = make_shared<Room>();

void Room::Enter(PlayerRef player)
{
	cout << "엔터 진입" << endl;
	// 1. 방 멤버로 등록
	_players[player->playerId] = player;

	//// 2. (입장한 플레이어에게) 현재 방 플레이어 전체 리스트 송신.
	Protocol::S_PLAYERLIST pkt;
	for (auto& kv : _players)
	{
		Protocol::Player* info = pkt.add_players();
		info->set_id(kv.second->playerId);
		info->set_name(kv.second->name);
		info->set_playertype(kv.second->type);
		info->set_posx(kv.second->posX);
		info->set_posy(kv.second->posY);
		info->set_posz(kv.second->posZ);
	}
	pkt.set_myplayerid(player->playerId); // 자기자신 표시
	SendBufferRef playerListBuffer = ClientPacketHandler::MakeSendBuffer(pkt);
	player->ownerSession->Send(playerListBuffer);

	cout << "플레이어 목록 전송" << endl;

	////// 3. (전체 멤버에게) 입장 브로드캐스트
	Protocol::S_BROADCAST_ENTER_GAME enterPkt;
	enterPkt.set_playerid(player->playerId);
	enterPkt.set_name(player->name);
	enterPkt.set_posx(player->posX);
	enterPkt.set_posy(player->posY);
	enterPkt.set_posz(player->posZ);
	SendBufferRef enterBuffer = ClientPacketHandler::MakeSendBuffer(enterPkt);

	BroadCast(enterBuffer);
	
}

void Room::Leave(PlayerRef player)
{
	// TODO Broadcasting?

	auto leavePlayerId = player->playerId;
	// 현재 룸에서 해당 플레이어 제거
	_players.erase(leavePlayerId);

	// 남은 모든 세션에 S_BROADCAST_LEAVE_GAME 전송
	Protocol::S_BROADCAST_LEAVE_GAME pkt;
	pkt.set_playerid(leavePlayerId);
	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(pkt);
	BroadCast(sendBuffer);
}

void Room::BroadCast(SendBufferRef sendBuffer)
{
	for (auto& s: _players)
	{
		s.second->ownerSession->Send(sendBuffer);
	}
}

