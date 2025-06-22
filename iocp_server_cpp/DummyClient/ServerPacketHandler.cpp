#include "pch.h"
#include "ServerPacketHandler.h"

PacketHandlerFunc GPacketHandler[UINT16_MAX];

bool Handle_INVALID(PacketSessionRef& session, BYTE* buffer, int32 len)
{
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);

	// TODO : log
	return true;
}

bool Handle_S_LOGIN(PacketSessionRef& session, Protocol::S_LOGIN& pkt)
{
	if (pkt.success() == false)
		return true;

	if (pkt.players().size() == 0)
	{
		// 캐릭터 생성창으로 이동
	}

	// 입장 UI 버튼 눌러서 게임 입장
	Protocol::C_ENTER_GAME enterGamePkt;
	enterGamePkt.set_playerindex(0); // 첫번째 캐릭터로 입장
	auto sendBuffer = ServerPacketHandler::MakeSendBuffer(enterGamePkt);
	session->Send(sendBuffer);

	return true;
}

bool Handle_S_ENTER_GAME(PacketSessionRef& session, Protocol::S_ENTER_GAME& pkt)
{
	return true;
}

bool Handle_S_BROADCAST_LEAVE_GAME(PacketSessionRef& session, Protocol::S_BROADCAST_LEAVE_GAME& pkt)
{
	return true;
}

bool Handle_S_BROADCAST_CHAT(PacketSessionRef& session, Protocol::S_BROADCAST_CHAT& pkt)
{
	std::cout << pkt.playerid() << " : " << pkt.msg() << " --> " << endl;
	return true;
}

bool Handle_S_RTT(PacketSessionRef& session, Protocol::S_RTT& pkt)
{
	return true;
}

bool Handle_S_PLAYERLIST(PacketSessionRef& session, Protocol::S_PLAYERLIST& pkt)
{
	std::cout << "\n=== PlayerList Packet Contents ===" << std::endl;
	std::cout << "My Player ID: " << pkt.myplayerid() << std::endl;
	std::cout << "Total Players: " << pkt.players_size() << std::endl;
	
	for (int i = 0; i < pkt.players_size(); i++)
	{
		const Protocol::Player& player = pkt.players(i);
		std::cout << "\nPlayer " << i + 1 << ":" << std::endl;
		std::cout << "  ID: " << player.id() << std::endl;
		std::cout << "  Name: " << player.name() << std::endl;
		std::cout << "  Type: " << player.playertype() << std::endl;
		std::cout << "  Position: (" << player.posx() << ", " << player.posy() << ", " << player.posz() << ")" << std::endl;
	}
	std::cout << "===============================\n" << std::endl;
	
	return true;
}

bool Handle_S_BROADCAST_ENTER_GAME(PacketSessionRef& session, Protocol::S_BROADCAST_ENTER_GAME& pkt)
{
	std::cout << "PlayerBroadcast Enter" << std::endl;
	return true;
}

bool Handle_S_BROADCAST_MOVE(PacketSessionRef& session, Protocol::S_BROADCAST_MOVE& pkt)
{
	return true;
}


//
//
//bool Handle_S_TEST(PacketSessionRef& session, Protocol::S_TEST& pkt)
//{
//
//	uint64 id = pkt.id();
//	uint32 hp = pkt.hp();
//	uint32 attack = pkt.attack();
//
//	cout << id << " " << hp << " " << attack << endl;
//
//	cout << "BUFSIZE : " << pkt.buffs_size() << endl;
//
//	for (auto& buf : pkt.buffs())
//	{
//		cout << "BUFINFO : " << buf.buffid() << " " << buf.remaintime() << endl;
//		cout << "VICTIMS : " << buf.victims_size() << endl;
//
//		for (auto& vic : buf.victims())
//		{
//			cout  << vic << " ";
//		}
//		cout << endl;
//	}
//
//	return true;
//}
//
//bool Handle_S_LOGIN(PacketSessionRef& session, Protocol::S_LOGIN& pkt)
//{
//	return true;
//}
