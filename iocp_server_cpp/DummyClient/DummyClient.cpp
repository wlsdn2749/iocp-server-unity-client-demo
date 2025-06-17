#include "pch.h"
#include <iostream>
#include "ThreadManager.h"
#include "Service.h"
#include "ServerSession.h"
#include "ServerPacketHandler.h"
#include "SessionManager.h"


int main()
{
	ServerPacketHandler::init();
	
	this_thread::sleep_for(1s);

	ClientServiceRef service = MakeShared<ClientService>(
		NetAddress(L"127.0.0.1", 8421),
		MakeShared<IocpCore>(),
		[] { return SessionManager::Instance().Generate(); },
		//MakeShared<ServerSession>, // TODO,  ()가 필요없는건, CreateSession으로 넘어가서 실행하기 때문
		5);

	ASSERT_CRASH(service->Start());
	for (int32 i = 0; i < 2; i++)
	{
		GThreadManager->Launch([=]()
			{
				while (true)
				{
					// Worker Thread가 Queue에서 Event를 꺼냄
					service->GetIocpCore()->Dispatch();
				}
			});
	}

	// 현재 접속한 모든 세션이 Move 패킷을 보냄
	for (int32 i = 0; i < 1; i++)
	{
		GThreadManager->Launch([]()
			{
				while (true)
				{
					SessionManager::Instance().SendForEach();
					std::this_thread::sleep_for(std::chrono::milliseconds(250));
				}
			});
	}
	Protocol::C_CHAT chatPkt;
	chatPkt.set_msg(u8"Hello World !");
	auto sendBuffer = ServerPacketHandler::MakeSendBuffer(chatPkt);

	while (true)
	{
		service->Broadcast(sendBuffer);
		this_thread::sleep_for(2s);
	}

	GThreadManager->Join();
}
