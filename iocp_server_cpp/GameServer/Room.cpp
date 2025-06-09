#include "pch.h"
#include "Room.h"

#include "Player.h"
#include "GameSession.h"

shared_ptr<Room> GRoom = make_shared<Room>();

void Room::Enter(PlayerRef player)
{
	_players[player->playerId] = player;
}

void Room::Leave(PlayerRef player)
{
	_players.erase(player->playerId);
}

void Room::BroadCast(SendBufferRef sendBuffer)
{
	for (auto& s: _players)
	{
		s.second->ownerSession->Send(sendBuffer);
	}
}

