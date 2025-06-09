#include "pch.h"
#include "GameSessionManager.h"

#include "../GameServer/GameSession.h"

GameSessionManager GSessionManager;

void GameSessionManager::Add(GameSessionRef session)
{
	WRITE_LOCK;
	_sessions.insert(session);
}

void GameSessionManager::Remove(GameSessionRef session)
{
	WRITE_LOCK;
	_sessions.erase(session);
}

void GameSessionManager::Broadcast(SendBufferRef sendBuffer)
{
	//vector<GameSessionRef> sessions;
	//{
	//	WRITE_LOCK;
	//	for (const auto& session : _sessions)
	//		sessions.push_back(session); // 복사
	//}

	//// 락 해제 후 안전하게 접근
	//for (auto& session : sessions)
	//{
	//	session->Send(sendBuffer);
	//}

	WRITE_LOCK;
	for (GameSessionRef session : _sessions)
	{
		//sessions를 for-each를 돌다가
		//sessions를 삭제하거나 수정하거나 하면,
		////_sessions자체가 오염될 수 있음
		/////_session.erase(session)가 생길 수 있고 결론적으로 문제가 생길 수 있음.
		session->Send(sendBuffer); // 불필요한 복사 X
	}
}
