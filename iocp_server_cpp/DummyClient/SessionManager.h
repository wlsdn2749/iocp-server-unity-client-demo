#pragma once

#include <random>
#include "ServerSession.h"            // PacketSession 파생
#include "Protocol.pb.h"
#include "ServerPacketHandler.h"

class SessionManager
{
public:
    static SessionManager& Instance()
    {
        static SessionManager inst;
        return inst;
    }

    /* 세션 생성(Connector 에서 호출) */
    ServerSessionRef Generate()
    {
        auto session = MakeShared<ServerSession>();

        WRITE_LOCK;
        _sessions.push_back(session);
        return session;
    }

    /* 주기적 MOVE 전송 */
    void SendForEach()
    {
        // 1) snapshot
        Vector<ServerSessionRef> snapshot;
        {
            READ_LOCK; // WRITE -> READ
            snapshot = _sessions;                 // 얕은 복사
        }

        // 2) 난수 생성기
        thread_local std::mt19937 gen{ std::random_device{}() };
        std::uniform_real_distribution<float> dis(-50.f, 50.f);

        // 3) 세션마다 MOVE
        for (auto& session : snapshot)
        {
            Protocol::C_MOVE mv;
            mv.set_posx(dis(gen));
            mv.set_posy(5.0f);
            mv.set_posz(dis(gen));

            auto sendBuf = ServerPacketHandler::MakeSendBuffer(mv);
            session->Send(sendBuf);
        }
    }

private:
    USE_LOCK;
    Vector<ServerSessionRef> _sessions;
    //std::vector<std::shared_ptr<ServerSession>> _sessions;
};
