#pragma once
#include "Protocol.pb.h"


using PacketHandlerFunc = std::function<bool(PacketSessionRef&, BYTE*, int32)>;
extern PacketHandlerFunc GPacketHandler[UINT16_MAX]; // 65535만큼의 배열 개수

enum : uint16
{
	PKT_C_LOGIN = 1000,
	PKT_S_LOGIN = 1001,
	PKT_C_ENTER_GAME = 1002,
	PKT_S_ENTER_GAME = 1003,
	PKT_C_CHAT = 1004,
	PKT_S_CHAT = 1005,
	PKT_S_PLAYERLIST = 1006,
	PKT_S_BROADCAST_ENTER_GAME = 1007,
	PKT_C_MOVE = 1008,
	PKT_S_BROADCAST_MOVE = 1009,

};

// Custom Handler : 직접 컨텐츠 작업자가 CPP를 만들어야함

bool Handle_INVALID(PacketSessionRef& session, BYTE* buffer, int32 len);
bool Handle_S_LOGIN(PacketSessionRef& session, Protocol::S_LOGIN& pkt);
bool Handle_S_ENTER_GAME(PacketSessionRef& session, Protocol::S_ENTER_GAME& pkt);
bool Handle_S_CHAT(PacketSessionRef& session, Protocol::S_CHAT& pkt);
bool Handle_S_PLAYERLIST(PacketSessionRef& session, Protocol::S_PLAYERLIST& pkt);
bool Handle_S_BROADCAST_ENTER_GAME(PacketSessionRef& session, Protocol::S_BROADCAST_ENTER_GAME& pkt);
bool Handle_S_BROADCAST_MOVE(PacketSessionRef& session, Protocol::S_BROADCAST_MOVE& pkt);

class ServerPacketHandler
{
public:

	static void init()
	{
		for (int32 i = 0; i < UINT16_MAX; i++)
		{
			GPacketHandler[i] = Handle_INVALID;
		}
		GPacketHandler[PKT_S_LOGIN] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::S_LOGIN>(Handle_S_LOGIN, session, buffer, len); };
		GPacketHandler[PKT_S_ENTER_GAME] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::S_ENTER_GAME>(Handle_S_ENTER_GAME, session, buffer, len); };
		GPacketHandler[PKT_S_CHAT] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::S_CHAT>(Handle_S_CHAT, session, buffer, len); };
		GPacketHandler[PKT_S_PLAYERLIST] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::S_PLAYERLIST>(Handle_S_PLAYERLIST, session, buffer, len); };
		GPacketHandler[PKT_S_BROADCAST_ENTER_GAME] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::S_BROADCAST_ENTER_GAME>(Handle_S_BROADCAST_ENTER_GAME, session, buffer, len); };
		GPacketHandler[PKT_S_BROADCAST_MOVE] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::S_BROADCAST_MOVE>(Handle_S_BROADCAST_MOVE, session, buffer, len); };
		
	}
	static bool HandlePacket(PacketSessionRef& session, BYTE* buffer, int32 len)
	{
		PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
		return GPacketHandler[header->id](session, buffer, len);
	}
	static SendBufferRef MakeSendBuffer(Protocol::C_LOGIN& pkt) { return MakeSendBuffer(pkt, PKT_C_LOGIN); };
	static SendBufferRef MakeSendBuffer(Protocol::C_ENTER_GAME& pkt) { return MakeSendBuffer(pkt, PKT_C_ENTER_GAME); };
	static SendBufferRef MakeSendBuffer(Protocol::C_CHAT& pkt) { return MakeSendBuffer(pkt, PKT_C_CHAT); };
	static SendBufferRef MakeSendBuffer(Protocol::C_MOVE& pkt) { return MakeSendBuffer(pkt, PKT_C_MOVE); };

private:
	template<typename PacketType, typename ProcessFunc>
	static bool HandlePacket(ProcessFunc func, PacketSessionRef& session, BYTE* buffer, int32 len)
	{
		PacketType pkt;
		if (pkt.ParseFromArray(buffer + sizeof(PacketHeader), len - sizeof(PacketHeader)) == false)
			return false;

		return func(session, pkt);
	}

	template<typename T>
	static SendBufferRef MakeSendBuffer(T& pkt, uint16 pktId)
	{
		const uint16 dataSize = static_cast<uint16>(pkt.ByteSizeLong()); // byte로 환산하면 얼마인가?

		const uint16 packetSize = dataSize + sizeof(PacketHeader);

		SendBufferRef sendBuffer = GSendBufferManager->Open(packetSize);

		PacketHeader* header = reinterpret_cast<PacketHeader*>(sendBuffer->Buffer());
		header->size = packetSize;
		header->id = pktId;

		ASSERT_CRASH(pkt.SerializeToArray(&header[1], dataSize));


		sendBuffer->Close(packetSize);
		return sendBuffer;
	}

};