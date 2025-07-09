#pragma once

#include "Session.h"
#include "PerformanceStats.h"

class GameSession : public PacketSession
{
public:
	enum class State : uint8
	{
		None, // TCP 연결 전 (가능한가?)...
		Connected, // TCP 연결 된 직후 -> OnConnected() 
		InGame, // 로그인 완료 -> ClientPacketHandler::C_Login() 에서 처리
		InRoom, // 룸에 접속함 -> ClientPacketHandler::C_ENTER_GAME() 에서 처리
		Disconnected // Room::OnDisConnected
	};

	State GetState() const { return _state.load(std::memory_order_acquire); }
	void  SetState(State s) { _state.store(s, std::memory_order_release); }

private:
	std::atomic<State> _state{ State::None };

public:
	~GameSession()
	{
		cout << "~GameSession" << endl;
	}

	virtual void OnConnected() override;
	virtual void OnDisconnected() override;
	virtual void OnRecvPacket(BYTE* buffer, int32 len) override;
	virtual void OnSend(int32 len) override;

public:
	Vector<PlayerRef> _players;
	PlayerRef _currentPlayer;
	// 이 위 두개는 Player와의 연결이 종료되면 삭제되면서 Player를
	// 물고 있던 참조카운트가 2개 빠지는데 이떄 비로소 player가 삭제된다.


	weak_ptr<class Room> _room;
};

