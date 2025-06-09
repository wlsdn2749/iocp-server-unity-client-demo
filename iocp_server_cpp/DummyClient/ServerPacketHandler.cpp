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

bool Handle_S_CHAT(PacketSessionRef& session, Protocol::S_CHAT& pkt)
{
	std::cout << pkt.msg() << endl;
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
