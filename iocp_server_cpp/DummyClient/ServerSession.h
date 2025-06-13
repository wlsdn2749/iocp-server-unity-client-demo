#pragma once
#include "Protocol.pb.h"
#include "ServerPacketHandler.h"

class ServerSession : public PacketSession
{
public:
	~ServerSession()
	{
		cout << "~ServerSession" << endl;
	}
	virtual void OnConnected() override
	{
		//cout << "Connected To Server" << endl;
		Protocol::C_LOGIN pkt;
		auto sendBuffer = ServerPacketHandler::MakeSendBuffer(pkt);
		Send(sendBuffer);

	}
	virtual void OnDisconnected() override
	{
		/*SessionManager::Instance().Remove(shared_from_this());*/
		//cout << "DisConnected " << endl;
	}
	virtual void OnRecvPacket(BYTE* buffer, int32 len) override
	{
		PacketSessionRef session = GetPacketSessionRef();
		PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);

		ServerPacketHandler::HandlePacket(session, buffer, len);

	}

	virtual void OnSend(int32 len) override
	{
		// Echo
		//cout << "OnSend Len = " << len << endl;
	}
};
