#pragma once
#include "Protocol.pb.h"
#include "SendBuffer.h"

namespace ProtobufSizeUtil
{
	inline constexpr auto MaxSendBufferSize = []() constexpr -> size_t {
		return SendBufferChunk::SEND_BUFFER_CHUNK_SIZE;
	};
	// TODO 이걸 자동화를 하든? 아니면 템플릿을 만들든..
	
    // PlayerMove의 직렬화 길이를 미리 잰 뒤 상수처럼 재사용
    inline size_t PerPlayerMoveBytes()
    {
        static const size_t k = [] {
			//Protocol::S_BROADCAST_MOVE movePkt;
			Protocol::PlayerMove mv;
			mv.set_playerid(123);

			Protocol::Vec3* pos = mv.mutable_pos(); // pos부분
			pos->set_x(1.0f);
			pos->set_y(2.0f);
			pos->set_z(3.0f);

			Protocol::PlayerMoveInput* playerMoveInput = mv.mutable_input();
			Protocol::Vec3* dir = playerMoveInput->mutable_dir();; // dir
			dir->set_x(1.0f);
			dir->set_y(2.0f);
			dir->set_z(3.0f);
			playerMoveInput->set_speed(1.1f);

			// 이부분 입력값이 0이면, Protobuf에서는 이를 직렬화 하지않음
			// 또한 Repeated 객체의 껍데기는 2B

			cout << "mv.ByteSizeLong()" << mv.ByteSizeLong() + 1;

            return mv.ByteSizeLong() + 1;    // +1 : repeated-tag(1B) --> 40정도
        }();
        return k;
    }

	inline size_t PerPlayerBytes()
	{
		static const size_t k = [] {

			Protocol::Player player;

			player.set_id(123);
			player.set_name("HIMYNAMEIS");
			player.set_playertype(Protocol::PLAYER_TYPE_HUMAN);
			player.set_posx(1.0f);
			player.set_posy(2.0f);
			player.set_posz(3.0f);

			// 이부분 입력값이 0이면, Protobuf에서는 이를 직렬화 하지않음
			// 또한 Repeated 객체의 껍데기는 2B

			cout << "player.ByteSizeLong()" << player.ByteSizeLong() + 1;

			return player.ByteSizeLong() + 1;    // +1 : repeated-tag(1B) --> 40정도
			}();
		return k;
	}
	inline size_t SeqFieldBytes(uint32 seq)
	{
		return 1 + 4; // uint32 변동길이 1~4	
	}

	inline size_t PlayerIdFieldBytes(uint64 playerId)
	{
		return 1 + 5; // uint64 변동길이 1~5
	}
	constexpr size_t kHeaderBytes = sizeof(PacketHeader);
	constexpr size_t kChunkLimit = SendBufferChunk::SEND_BUFFER_CHUNK_SIZE;

	inline size_t PredictMovePacketBytes(uint32 seq, int playerCnt)
	{
		return kHeaderBytes + SeqFieldBytes(seq)
			+ PerPlayerMoveBytes() * playerCnt;
	}

	/*----------------------------------------------------
	  현재 seq 기준, 한 무브 패킷에 담을 수 있는 최대 플레이어 수
	----------------------------------------------------*/
	inline int MaxPlayersPerMovePacket(uint32 seq)
	{
		size_t room = kChunkLimit - kHeaderBytes - SeqFieldBytes(seq);
		return static_cast<int>(room / PerPlayerMoveBytes());
	}


	/*----------------------------------------------------
	  현재 id 기준, 한 플레이어 전송 패킷에 담을 수 있는 최대 플레이어 수
	----------------------------------------------------*/
	inline int MaxPlayersPerPlayerListPacket(uint64 playerId)
	{
		size_t room = kChunkLimit - kHeaderBytes - PlayerIdFieldBytes(playerId);
		return static_cast<int>(room / PerPlayerBytes());
	}
}