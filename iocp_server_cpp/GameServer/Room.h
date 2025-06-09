#pragma once
#include "Job.h"
#include "JobQueue.h"

class Room : public JobQueue
{

public:
	// 싱글 스레드 환경인 마냥 처리
	// Job방식을 통해 특정 스레드 1개가 Dispatch하ㅁ로
	// 싱글 스레드 환경인것처럼 처리해도 겹치지 않음
	void Enter(PlayerRef player);
	void Leave(PlayerRef player);
	void BroadCast(SendBufferRef sendBuffer);

private:
	map<uint64, PlayerRef> _players;
};

extern shared_ptr<Room> GRoom;
