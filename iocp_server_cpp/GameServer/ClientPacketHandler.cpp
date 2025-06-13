#include "pch.h"
#include "ClientPacketHandler.h"

#include "GameSession.h"
#include "Player.h"
#include "Room.h"

// Random Code
#include <random>
static std::random_device rd;
static std::mt19937 gen(rd());
std::uniform_real_distribution<float> dis(-50.0f, 50.0f); // ~50,50


PacketHandlerFunc GPacketHandler[UINT16_MAX];

// 직접 컨텐츠 작업자가 만들어야함

bool Handle_INVALID(PacketSessionRef& session, BYTE* buffer, int32 len)
{
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);

	// TODO : log
	return true;
}

bool Handle_C_LOGIN(PacketSessionRef& session, Protocol::C_LOGIN& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);

	//// TODO : validation 체크

	Protocol::S_LOGIN loginPkt;
	loginPkt.set_success(true);
	static Atomic<uint64> idGenerator = 1;

	{
		auto player = loginPkt.add_players();
		player->set_name(u8"Tommy");
		player->set_playertype(Protocol::PLAYER_TYPE_KNIGHT);
		player->set_posx(dis(gen));
		player->set_posy(0);
		player->set_posz(dis(gen));

		PlayerRef playerRef = MakeShared<Player>();
		playerRef->playerId = idGenerator++;
		playerRef->name = player->name();
		playerRef->type = player->playertype();
		playerRef->ownerSession = gameSession;

		playerRef->posX = player->posx();
		playerRef->posY = player->posy();
		playerRef->posZ = player->posz();

		gameSession->_players.push_back(playerRef);
	}

	auto sendBuffer = ClientPacketHandler::MakeSendBuffer(loginPkt);
	session->Send(sendBuffer);

	return true;
}

bool Handle_C_ENTER_GAME(PacketSessionRef& session, Protocol::C_ENTER_GAME& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);

	uint64 index = pkt.playerindex();

	// TODO : validation

	gameSession->_currentPlayer = gameSession->_players[index]; // READ_ONLY??
	gameSession->_room = GRoom;

	//GRoom.Enter(player);
	//JobRef enterJob = MakeShared<EnterJob>(GRoom, player);
	GRoom->DoAsync(&Room::Enter, gameSession->_currentPlayer);

	Protocol::S_ENTER_GAME enterGamePkt;
	enterGamePkt.set_success(true);
	auto sendBuffer = ClientPacketHandler::MakeSendBuffer(enterGamePkt);
	gameSession->_currentPlayer->ownerSession->Send(sendBuffer);

	std::cout << "[Server Action] Client Room 접속 완료" << std::endl;

	return true;
}

bool Handle_C_CHAT(PacketSessionRef& session, Protocol::C_CHAT& pkt)
{
	std::cout << pkt.msg() << endl;

	Protocol::S_CHAT chatPkt;
	chatPkt.set_msg(pkt.msg());
	auto sendBuffer = ClientPacketHandler::MakeSendBuffer(chatPkt);

	GRoom->BroadCast(sendBuffer);

	return true;
}
