#pragma once
#include "Protocol.pb.h"


using PacketHandlerFunc = std::function<bool(PacketSessionRef&, BYTE*, int32)>;
extern PacketHandlerFunc GPacketHandler[UINT16_MAX]; // 65535만큼의 배열 개수

enum : uint16
{
	PKT_C_REGISTER = 1000,
	PKT_S_REGISTER = 1001,
	PKT_C_LOGIN = 1002,
	PKT_S_LOGIN = 1003,
	PKT_C_ENTER_GAME = 1004,
	PKT_S_ENTER_GAME = 1005,
	PKT_C_LEAVE_GAME = 1006,
	PKT_S_BROADCAST_LEAVE_GAME = 1007,
	PKT_S_PLAYERLIST = 1008,
	PKT_S_BROADCAST_ENTER_GAME = 1009,
	PKT_C_MOVE = 1010,
	PKT_S_BROADCAST_MOVE = 1011,
	PKT_C_CHAT = 1012,
	PKT_S_BROADCAST_CHAT = 1013,
	PKT_C_RTT = 1014,
	PKT_S_RTT = 1015,

};

// Custom Handler : 직접 컨텐츠 작업자가 CPP를 만들어야함

bool Handle_INVALID(PacketSessionRef& session, BYTE* buffer, int32 len);
bool Handle_C_REGISTER(PacketSessionRef& session, Protocol::C_REGISTER& pkt);
bool Handle_C_LOGIN(PacketSessionRef& session, Protocol::C_LOGIN& pkt);
bool Handle_C_ENTER_GAME(PacketSessionRef& session, Protocol::C_ENTER_GAME& pkt);
bool Handle_C_LEAVE_GAME(PacketSessionRef& session, Protocol::C_LEAVE_GAME& pkt);
bool Handle_C_MOVE(PacketSessionRef& session, Protocol::C_MOVE& pkt);
bool Handle_C_CHAT(PacketSessionRef& session, Protocol::C_CHAT& pkt);
bool Handle_C_RTT(PacketSessionRef& session, Protocol::C_RTT& pkt);

class ClientPacketHandler
{
public:

	static void init()
	{
		for (int32 i = 0; i < UINT16_MAX; i++)
		{
			GPacketHandler[i] = Handle_INVALID;
		}
		GPacketHandler[PKT_C_REGISTER] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::C_REGISTER>(Handle_C_REGISTER, session, buffer, len); };
		GPacketHandler[PKT_C_LOGIN] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::C_LOGIN>(Handle_C_LOGIN, session, buffer, len); };
		GPacketHandler[PKT_C_ENTER_GAME] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::C_ENTER_GAME>(Handle_C_ENTER_GAME, session, buffer, len); };
		GPacketHandler[PKT_C_LEAVE_GAME] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::C_LEAVE_GAME>(Handle_C_LEAVE_GAME, session, buffer, len); };
		GPacketHandler[PKT_C_MOVE] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::C_MOVE>(Handle_C_MOVE, session, buffer, len); };
		GPacketHandler[PKT_C_CHAT] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::C_CHAT>(Handle_C_CHAT, session, buffer, len); };
		GPacketHandler[PKT_C_RTT] = [](PacketSessionRef& session, BYTE* buffer, int32 len) {return HandlePacket<Protocol::C_RTT>(Handle_C_RTT, session, buffer, len); };
		
	}
	static bool HandlePacket(PacketSessionRef& session, BYTE* buffer, int32 len)
	{
		PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
		return GPacketHandler[header->id](session, buffer, len);
	}
	static SendBufferRef MakeSendBuffer(Protocol::S_REGISTER& pkt) { return MakeSendBuffer(pkt, PKT_S_REGISTER); };
	static SendBufferRef MakeSendBuffer(Protocol::S_LOGIN& pkt) { return MakeSendBuffer(pkt, PKT_S_LOGIN); };
	static SendBufferRef MakeSendBuffer(Protocol::S_ENTER_GAME& pkt) { return MakeSendBuffer(pkt, PKT_S_ENTER_GAME); };
	static SendBufferRef MakeSendBuffer(Protocol::S_BROADCAST_LEAVE_GAME& pkt) { return MakeSendBuffer(pkt, PKT_S_BROADCAST_LEAVE_GAME); };
	static SendBufferRef MakeSendBuffer(Protocol::S_PLAYERLIST& pkt) { return MakeSendBuffer(pkt, PKT_S_PLAYERLIST); };
	static SendBufferRef MakeSendBuffer(Protocol::S_BROADCAST_ENTER_GAME& pkt) { return MakeSendBuffer(pkt, PKT_S_BROADCAST_ENTER_GAME); };
	static SendBufferRef MakeSendBuffer(Protocol::S_BROADCAST_MOVE& pkt) { return MakeSendBuffer(pkt, PKT_S_BROADCAST_MOVE); };
	static SendBufferRef MakeSendBuffer(Protocol::S_BROADCAST_CHAT& pkt) { return MakeSendBuffer(pkt, PKT_S_BROADCAST_CHAT); };
	static SendBufferRef MakeSendBuffer(Protocol::S_RTT& pkt) { return MakeSendBuffer(pkt, PKT_S_RTT); };

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