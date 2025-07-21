#include "pch.h"
#include "ClientPacketHandler.h"
#include "GameSession.h"
#include "LoginService.h"
#include "Player.h"
#include "Room.h"
#include "PrometheusMetrics.h"
#include "RegisterService.h"

// Prometheus 메트릭 (외부 선언)
extern PrometheusMetrics* GPrometheusMetrics;

// PacketId를 통해 매핑되는 Wrappers
PacketHandlerFunc GPacketHandler[UINT16_MAX];

// 직접 컨텐츠 작업자가 만들어야함
bool Handle_INVALID(PacketSessionRef& session, BYTE* buffer, int32 len)
{
	PacketHeader* header = reinterpret_cast<PacketHeader*>(buffer);
	cout << "INVALID PACKET ID" << endl;
	return true;
}

bool Handle_C_REGISTER(PacketSessionRef& session, Protocol::C_REGISTER& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);
	String email = StrToWstr(pkt.email());
	String pw = StrToWstr(pkt.pw());

	Protocol::RegisterResult result = {};
	int32 accountId = -1;
	RegisterService::Instance().RequestRegister(session, email, pw, OUT result, OUT accountId);

	// 여기까지 오면, result, accountId가 채워져있음.
	Protocol::S_REGISTER registerPkt;

	registerPkt.set_result(result);
	registerPkt.set_accountid(accountId);
	auto sendBuffer = ClientPacketHandler::MakeSendBuffer(registerPkt);
	session->Send(sendBuffer);
	return true;
}

bool Handle_C_LOGIN(PacketSessionRef& session, Protocol::C_LOGIN& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);

	// Prometheus 메트릭 업데이트
	if (GPrometheusMetrics) {
		GPrometheusMetrics->IncrementPacketsReceived();
		GPrometheusMetrics->IncrementConnectedClients();
	}

	//// TODO : validation 체크
	int result = 0;
	Protocol::LoginResult loginResult = {};
	String email = StrToWstr(pkt.email());
	String pw = StrToWstr(pkt.pw());

	LoginService::Instance().RequestLogin(session, email, pw, OUT result);


	// 로그인
	// result = 0 : 성공
	// result = 4 : Server Error
	// result = 3 : 비밀번호 일치 X
	// result = 2 : Email 등록 X

	loginResult = static_cast<Protocol::LoginResult>(result); // DB작업에 실행된 기존 result값을 switch에 사용하기 위해 바꿈
	switch (loginResult)
	{
	case Protocol::LoginResult::LOGIN_EMAIL_NOT_FOUND:
	case Protocol::LoginResult::LOGIN_PW_MISMATCH:
	case Protocol::LoginResult::LOGIN_SERVER_ERROR:
	{
		cout << "로그인 실패" << endl;
		Protocol::S_LOGIN loginPkt;
		loginPkt.set_result(loginResult);
		auto sendBuffer = ClientPacketHandler::MakeSendBuffer(loginPkt);
		session->Send(sendBuffer);

		return true;
	}

		// -------------  여기 아래는 로그인 성공한 경우. -------------------
	case Protocol::LoginResult::LOGIN_SUCCESS:
	default:
	{
		gameSession->SetState(GameSession::State::InGame);

		Protocol::S_LOGIN loginPkt;
		loginPkt.set_result(loginResult);
		static Atomic<uint64> idGenerator = 1;


		// 만약 플레이어를 DB에서 가져올거면
		// player도 넘겨야할듯? 넘기든가 받든가.

		{
			auto player = loginPkt.add_players();
			player->set_name(u8"Tommy");
			player->set_playertype(pkt.type());
			player->set_posx(0);
			player->set_posy(10);
			player->set_posz(0);

			PlayerRef playerRef = MakeShared<Player>();
			playerRef->playerId = idGenerator++;
			playerRef->name = player->name();
			playerRef->type = player->playertype();
			playerRef->ownerSession = gameSession;

			playerRef->posX = player->posx();
			playerRef->posY = player->posy();
			playerRef->posZ = player->posz();

			gameSession->_players.push_back(playerRef);
		}

		auto sendBuffer = ClientPacketHandler::MakeSendBuffer(loginPkt);
		session->Send(sendBuffer);

		return true;
	}
	}
}

bool Handle_C_ENTER_GAME(PacketSessionRef& session, Protocol::C_ENTER_GAME& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);

	uint64 index = pkt.playerindex();

	// TODO : validation

	gameSession->_currentPlayer = gameSession->_players[index]; // READ_ONLY??
	gameSession->_room = GRoom;

	gameSession->SetState(GameSession::State::InRoom);
	GRoom->DoAsync(&Room::Enter, gameSession->_currentPlayer);

	Protocol::S_ENTER_GAME enterGamePkt;
	enterGamePkt.set_success(true);
	auto sendBuffer = ClientPacketHandler::MakeSendBuffer(enterGamePkt);
	gameSession->_currentPlayer->ownerSession->Send(sendBuffer);

	std::cout << "[Server Action] Client Room 접속 완료" << std::endl;

	return true;
}

bool Handle_C_LEAVE_GAME(PacketSessionRef& session, Protocol::C_LEAVE_GAME& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);
	GRoom->DoAsync(&Room::Leave, gameSession->_currentPlayer);
	// 룸을 나감 -> 룸에서 제거, 브로드캐스팅
	return true;
}

bool Handle_C_CHAT(PacketSessionRef& session, Protocol::C_CHAT& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);
	//std::cout << pkt.msg() << endl;

	if (gameSession->GetState() != GameSession::State::InRoom)
		return false; // Room에 들어오지 않았는데, C_Move를 핸들링하면, gameSession에 없는데 동작하므로 에러.

	// Prometheus 메트릭 업데이트
	if (GPrometheusMetrics) {
		GPrometheusMetrics->IncrementPacketsReceived();
		GPrometheusMetrics->IncrementChatPackets();
	}


	GRoom->DoAsync(&Room::AddChat, gameSession, pkt); // 룸 외부에서 처리할때는 반드시 DoAsync 사용

	return true;
}

bool Handle_C_RTT(PacketSessionRef& session, Protocol::C_RTT& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);
	
	// 현재 서버 시간을 마이크로초 단위로 측정
	auto now = std::chrono::high_resolution_clock::now();
	auto duration = now.time_since_epoch();
	auto microseconds = std::chrono::duration_cast<std::chrono::microseconds>(duration).count();
	
	// S_RTT 패킷 생성 및 응답
	Protocol::S_RTT rttPkt;
	rttPkt.set_clienttime(pkt.clienttime());  // 클라이언트가 보낸 시간을 그대로 반환
	rttPkt.set_servertime(microseconds);      // 서버 현재 시간
	
	auto sendBuffer = ClientPacketHandler::MakeSendBuffer(rttPkt);
	session->Send(sendBuffer);
	
	// Prometheus 메트릭 업데이트 (RTT 계산은 클라이언트에서 수행)
	if (GPrometheusMetrics) {
		GPrometheusMetrics->IncrementPacketsReceived();
		GPrometheusMetrics->IncrementRttPackets();

		GPrometheusMetrics->IncrementPacketsSent("rtt");
	}
	
	return true;
}

bool Handle_C_MOVE(PacketSessionRef& session, Protocol::C_MOVE& pkt)
{
	GameSessionRef gameSession = static_pointer_cast<GameSession>(session);

	if(gameSession->GetState() != GameSession::State::InRoom)
		return false; 

	Protocol::PlayerMoveInput inp = pkt.input();
	Protocol::Vec3 dir = inp.dir();
	float speed = inp.speed();

	// TODO 검증
	// Prometheus 메트릭 업데이트
	if (GPrometheusMetrics) {
		GPrometheusMetrics->IncrementPacketsReceived();
		GPrometheusMetrics->IncrementMovePackets();
	}
	
	GRoom->DoAsync(&Room::UpdateMoveInput,
					gameSession->_currentPlayer->playerId,
					std::move(inp));
	return true;
}
