#include "pch.h"
#include "Room.h"

#include "ClientPacketHandler.h"
#include "Player.h"
#include "GameSession.h"
#include "Protocol.pb.h"
#include "SendBuffer.h"
#include "ProtobufSizeUtil.h"

shared_ptr<Room> GRoom = make_shared<Room>();

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
	uint64 now = ::GetTickCount64();
	float realDt = (now - _lastTickMs) * 0.001f;

	// Option 2. Catch-up Loop
	while (realDt >= kFixedDt)
	{
		ProcessTick(kFixedDt); // dt는 항상 0.05
		_lastTickMs += kFixedMs; // 타임라인 50ms 앞으로
		realDt -= kFixedDt; // 남은 시간 차감
	}
	ReserveNextTick();
}
void Room::ReserveNextTick()
{
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
	BroadCastMoveSnap(dt);
	BroadCastChatSnap(dt);
}


void Room::BroadCastMoveSnap(float dt)
{
	const int   totalPlayers = static_cast<int>(_players.size());
	const int   batch = ProtobufSizeUtil::MaxPlayersPerMovePacket(_moveSeq + 1); // 자동 batch
	auto        it = _players.begin();

	const int seq = ++_moveSeq;

	while (it != _players.end())
	{
		Protocol::S_BROADCAST_MOVE movePkt;
		movePkt.set_seq(seq);

		int pushed = 0;
		while (pushed < batch && it != _players.end())
		{
			PlayerRef p = (it++)->second;

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

			++pushed;
		}

		//cout << pushed << "만큼 보냄 " << endl;
		auto buf = ClientPacketHandler::MakeSendBuffer(movePkt);
		BroadCast(buf);
	}
}
void Room::BroadCastChatSnap(float dt)
{
	if (_pendingChats.empty()) return;
	auto kMaxPayloadSize = ProtobufSizeUtil::kChunkLimit - sizeof(PacketHeader);
	auto it = _pendingChats.begin();

	const int seq = ++_chatSeq;

	while (it != _pendingChats.end())
	{
		Protocol::S_BROADCAST_CHAT broadcastPkt;
		broadcastPkt.set_seq(seq);

		while (it != _pendingChats.end())
		{
			/* 1) 용량 계산을 먼저 */
			const auto& [sess, cchat] = *it;

			Protocol::ChatMsg* chat = broadcastPkt.add_chats();
			chat->set_playerid(sess->_currentPlayer->playerId);
			chat->set_msg(cchat.msg());

			// 초과 체크
			size_t curSize = broadcastPkt.ByteSizeLong();
			if (curSize > kMaxPayloadSize)
			{
				// 방금 추가한 거 되돌리기
				broadcastPkt.mutable_chats()->RemoveLast();
				break;
			}

			++it;
		}

		auto buf = ClientPacketHandler::MakeSendBuffer(broadcastPkt);
		BroadCast(buf);
	}

	// 모두 보냈으면 클리어. 
	_pendingChats.clear();
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
void Room::AddChat(GameSessionRef session, Protocol::C_CHAT pkt)
{
	_pendingChats.emplace_back(session, pkt);
}

void Room::Enter(PlayerRef enteringPlayer)
{
	const int   totalPlayers = static_cast<int>(_players.size());
	const int   batch = ProtobufSizeUtil::MaxPlayersPerPlayerListPacket(_moveSeq + 1); // 자동 batch
	auto        it = _players.begin();

	// 1. 방 멤버로 등록
	_players[enteringPlayer->playerId] = enteringPlayer;

	// 2. (입장한 플레이어에게) 현재 방 플레이어 전체 리스트 송신.

	while (it != _players.end())
	{
		Protocol::S_PLAYERLIST pkt;
		pkt.set_myplayerid(enteringPlayer->playerId);

		int pushed = 0;
		while (pushed < batch && it != _players.end())
		{
			PlayerRef p = (it++)->second;

			Protocol::Player* info = pkt.add_players();

			info->set_id(p->playerId);
			info->set_name(p->name);
			info->set_playertype(p->type);
			info->set_posx(p->posX);
			info->set_posy(p->posY);
			info->set_posz(p->posZ);

			++pushed;
		}

		//cout << pushed << "만큼 플레이어 목록 전송 " << endl;
		auto playerListBuffer = ClientPacketHandler::MakeSendBuffer(pkt);
		enteringPlayer->ownerSession->Send(playerListBuffer);
	}


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







