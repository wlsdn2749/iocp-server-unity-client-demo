#include "pch.h"
#include "Room.h"

#include "ClientPacketHandler.h"
#include "Player.h"
#include "GameSession.h"
#include "Protocol.pb.h"

shared_ptr<Room> GRoom = make_shared<Room>();

void Room::Enter(PlayerRef player)
{
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

	////// 3. (전체 멤버에게) 입장 브로드캐스트
	Protocol::S_BROADCAST_ENTER_GAME enterPkt;
	enterPkt.set_playerid(player->playerId);
	enterPkt.set_name(player->name);
	enterPkt.set_posx(player->posX);
	enterPkt.set_posy(player->posY);
	enterPkt.set_posz(player->posZ);
	SendBufferRef enterBuffer = ClientPacketHandler::MakeSendBuffer(enterPkt);

	//GRoom->DoAsync(&Room::BroadCast, enterBuffer);
	BroadCast(enterBuffer);
	
}

void Room::Leave(PlayerRef player)
{
	// TODO Broadcasting?
	_players.erase(player->playerId);
}

void Room::BroadCast(SendBufferRef sendBuffer)
{
	// Todo 이 부분을 잘 수정해야 함, 이부분 만 수정하면
	// 잘될거같은데.. 흠....
	// 이게 원본코드
	for (auto& s: _players)
	{
		s.second->ownerSession->Send(sendBuffer);
	}
}

