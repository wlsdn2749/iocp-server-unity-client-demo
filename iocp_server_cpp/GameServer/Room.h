#pragma once
#include "Job.h"
#include "JobQueue.h"

class Room : public JobQueue
{
/*----------Room Tick-----------*/
public:
	void StartTick(); // 최초 1번만 호출
private:
	void OnTick(); // 예약 Job이 실행하는 함수
	void ReserveNextTick();
	void BroadCastMoveSnap(float dt); // BroadCast를 Snap하는 함수
	// 다음 Tick 예약
	void ProcessTick(float dt); // 기존 로직

public:
	// 게임 데이터 로직 처리
	void UpdateMoveInput(uint64 pid, Protocol::PlayerMoveInput inp);
	
public:
	// 싱글 스레드 환경인 마냥 처리
	// Job방식을 통해 특정 스레드 1개가 Dispatch하ㅁ로
	// 싱글 스레드 환경인것처럼 처리해도 겹치지 않음
	void Enter(PlayerRef player);
	void Leave(PlayerRef player);
	void BroadCast(SendBufferRef sendBuffer);

private:
	map<uint64, PlayerRef> _players; // playerId -> player Object
	uint64 _lastTickMs = 0;
	uint32 _moveSeq = 0;
	const float kWorldLimit = 90.f; // 벽은 90으로 고정
	const float kFixedDt = 0.05f; // 클라와 같은 Tick
	const uint64 kFixedMs = 50;
	

};

extern shared_ptr<Room> GRoom;
