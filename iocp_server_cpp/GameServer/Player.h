#pragma once
class Player
{
public:
	uint64						playerId = 0;
	string						name;
	Protocol::PlayerType		type = Protocol::PLAYER_TYPE_NONE;
	GameSessionRef				ownerSession; // Cycle Check TODO

public:
	float						posX = 0;
	float						posY = 0;
	float						posZ = 0;

public:
	float						dirX = 0;
	float						dirY = 0;
	float						dirZ = 0;
	float						speed = 0;
};

