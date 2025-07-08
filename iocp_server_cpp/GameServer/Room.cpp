#include "pch.h"
#include "Room.h"

#include "ClientPacketHandler.h"
#include "Player.h"
#include "GameSession.h"
#include "Protocol.pb.h"

shared_ptr<Room> GRoom = make_shared<Room>();

void Room::Enter(PlayerRef enteringPlayer)
{
	cout << "엔터 진입" << endl;
	// 1. 방 멤버로 등록
	_players[enteringPlayer->playerId] = enteringPlayer;

	//// 2. (입장한 플레이어에게) 현재 방 플레이어 전체 리스트 송신.
	Protocol::S_PLAYERLIST pkt;
	for (const auto& kv : _players)
	{
		const PlayerRef& p = kv.second;          // 가독성을 위해 별칭
		Protocol::Player* info = pkt.add_players();

        info->set_id        (p->playerId);
        info->set_name      (p->name);
        info->set_playertype(p->type);
        info->set_posx      (p->posX);
        info->set_posy      (p->posY);
        info->set_posz      (p->posZ);

	    std::cout << "[" << "PLAYERLIST" << "] id=" << p->playerId
	              << " | name=\"" << p->name << "\""
	              << " | type="   << static_cast<int>(p->type)
	              << " | pos=("   << p->posX << ", "
	                              << p->posY << ", "
	                              << p->posZ << ")\n";
	}
	pkt.set_myplayerid(enteringPlayer->playerId);
	SendBufferRef playerListBuffer = ClientPacketHandler::MakeSendBuffer(pkt);
	enteringPlayer->ownerSession->Send(playerListBuffer);

	cout << "플레이어 목록 전송" << endl;

	////// 3. (전체 멤버에게) 입장 브로드캐스트
	Protocol::S_BROADCAST_ENTER_GAME enterPkt;
	auto* myInfo = enterPkt.mutable_player();
	myInfo->set_id(enteringPlayer->playerId);
	myInfo->set_name(enteringPlayer->name);
	myInfo->set_playertype(enteringPlayer->type);
	myInfo->set_posx(enteringPlayer->posX);
	myInfo->set_posy(enteringPlayer->posY);
	myInfo->set_posz(enteringPlayer->posZ);

	SendBufferRef enterBuffer = ClientPacketHandler::MakeSendBuffer(enterPkt);
	BroadCast(enterBuffer);
	
}

void Room::Leave(PlayerRef player)
{
	// TODO Broadcasting?

	auto leavePlayerId = player->playerId;
	// 현재 룸에서 해당 플레이어 제거
	_players.erase(leavePlayerId);

	// 남은 모든 세션에 S_BROADCAST_LEAVE_GAME 전송
	Protocol::S_BROADCAST_LEAVE_GAME pkt;
	pkt.set_playerid(leavePlayerId);
	SendBufferRef sendBuffer = ClientPacketHandler::MakeSendBuffer(pkt);
	BroadCast(sendBuffer);
}


void Room::BroadCast(SendBufferRef sendBuffer)
{
	for (auto& s: _players)
	{
		s.second->ownerSession->Send(sendBuffer);
	}
}

void Room::UpdateMoveInput(uint64 pid, Protocol::PlayerMoveInput inp)
{
	auto it = _players.find(pid);
	if (it == _players.end()) return;

	PlayerRef p = it->second;
	p->dirX = inp.dir().x();
	p->dirY = inp.dir().y();
	p->dirZ = inp.dir().z();
	p->speed = inp.speed();
}

void Room::StartTick()
{
	_lastTickMs = ::GetTickCount64();
	ReserveNextTick(); // 첫 예약
}

void Room::OnTick()
{

	// 50ms 고정이 아니라 왜 실제 시간을 재서 쓰는가?
	// OS 스케쥴러에 의해 실제 50ms가 아니라, 42, 61, 58 이런식으로 변동이됨.
	// 그럴때, 서버가 50ms로 고정하게 되면, 서버에서 덜 이동하거나 더 이동하거나가 됨
	//
	uint64 now = ::GetTickCount64();
	float realDt = (now - _lastTickMs) * 0.001f;

	// Option 1. 단순 클램핑 -> 한 프레임에 최대 0.05초만 이동 나머지는 버림.
	//float dt = std::min<float>(realDt, kFixedDt); 
	//_lastTickMs = now;
	//ProcessTick(dt);
	//ReserveNextTick();

	// Option 2. Catch-up Loop
	while (realDt >= kFixedDt)
	{
		ProcessTick(kFixedDt); // dt는 항상 0.05
		_lastTickMs += kFixedMs; // 타임라인 50ms 앞으로
		realDt		-= kFixedDt; // 남은 시간 차감
	}
	ReserveNextTick();


}

void Room::ReserveNextTick()
{	

	// Option 1: 고정시간으로 50ms 매핑
	//std::weak_ptr<Room> weakSelf =
	//	std::static_pointer_cast<Room>(shared_from_this());

	//JobRef tickJob = MakeShared<Job>(
	//	[weakSelf]()
	//	{
	//		if(auto self = weakSelf.lock())
	//			self->OnTick();
	//	}
	//);

	//// JobTimer에 예약
	//GJobTimer->Reserve(kFixedMs, shared_from_this(), tickJob);


	// Option 2: Catch-Up Loop
	uint64 nextDueMs = _lastTickMs + kFixedMs;
	uint64 now = ::GetTickCount64();
	uint64 delayMs = (nextDueMs > now) ?          // 아직 시각이 안 왔으면
		(nextDueMs - now)          //   남은 시간만큼 기다리고
		: 0;

	std::weak_ptr<Room> weakSelf =
		std::static_pointer_cast<Room>(shared_from_this());

	JobRef tickJob = MakeShared<Job>(
		[weakSelf]()
		{
			if (auto self = weakSelf.lock())
				self->OnTick();
		}
	);

	GJobTimer->Reserve(delayMs, shared_from_this(), tickJob);
}

void Room::ProcessTick(float dt)
{

#pragma region 이동 브로드 캐스팅
	/*-----------Server-Side Player Move Broadcasting---------------------*/
	Protocol::S_BROADCAST_MOVE movePkt;

	for (auto& kv : _players)
	{
		PlayerRef p = kv.second;

		/* 위치 적분 */
		p->posX += p->dirX * p->speed * dt;
		p->posY += p->dirY * p->speed * dt;
		p->posZ += p->dirZ * p->speed * dt;
		/* 2) **경계 클램프**  ------------------------------*/
		p->posX = std::clamp(p->posX, -kWorldLimit, kWorldLimit);
		p->posZ = std::clamp(p->posZ, -kWorldLimit, kWorldLimit);


		/* 패킷 하나에 여러 플레이어를 담는다면 add_players() 식으로 */
		Protocol::PlayerMove* info = movePkt.add_playermoves();   // or 새로 만드는 방식
		info->set_playerid(p->playerId);

		Protocol::Vec3* pos = info->mutable_pos(); // pos부분
		pos->set_x(p->posX);
		pos->set_y(p->posY);
		pos->set_z(p->posZ);
		
		Protocol::PlayerMoveInput* playerMoveInput = info->mutable_input();
		Protocol::Vec3* dir = playerMoveInput->mutable_dir();; // dir
		dir->set_x(p->dirX);
		dir->set_y(p->dirY);
		dir->set_z(p->dirZ);
		playerMoveInput->set_speed(p->speed);

		/*std::cout
			<< " | pos=(" 
			<< p->posX << "," << p->posY << "," << p->posZ << ")"
			<< " | dir=(" << p->dirX << "," << p->dirY << "," << p->dirZ << ")"
			<< '\n';*/
	}

	/* 모두 담았으면 브로드캐스트 */
	auto buf = ClientPacketHandler::MakeSendBuffer(movePkt);
	BroadCast(buf);

#pragma endregion

}



