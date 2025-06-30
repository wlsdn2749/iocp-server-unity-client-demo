#include "pch.h"
#include "RegisterService.h"
#include "DBConnectionPool.h"
#include "GlobalQueue.h"
#include "JobQueue.h"
#include "Genprocedures.h"
#include <random>
#include <openssl/evp.h>

#include "ClientPacketHandler.h"
#include "Protocol.pb.h"
#include "ThreadManager.h"

void RegisterService::RequestRegister(SessionRef session, wstring_view email, wstring_view plainPw, OUT Protocol::RegisterResult& result, OUT int32& accountId)
{
	Bytes16 salt = MakeSalt();
	Bytes64 hash = CalcPwHash(plainPw, salt);

    // DB 작업을 직접 GGlobalQueue에 Push
    //auto tempJobQueue = MakeShared<JobQueue>();
    //tempJobQueue->DoAsync(&DoDbWork, session, wstring(email), hash, salt);

    //DoAsync(&RegisterService::DoDbWork, session, wstring(email), hash, salt);
	// TODO 여기 DoAsync부분 수정 필요함

    // 1. 그냥 Network Thread가 DB까지 같이 처리
    DoDbWork(session, wstring(email), hash, salt, OUT result, OUT accountId);
    
}

Bytes16 RegisterService::MakeSalt() const
{
    std::random_device rd;
    std::mt19937 gen(rd());
    std::uniform_int_distribution<uint32> dis(0, 255);
    
    Bytes16 salt;
    for (int i = 0; i < 16; ++i) {
        salt[i] = static_cast<BYTE>(dis(gen));
    }
    return salt;
}

Bytes64 RegisterService::CalcPwHash(wstring_view pw, const Bytes16& salt) const
{
    /* 1) pw ‖ salt 를 한 버퍼에 붙이기 */
    std::vector<uint8_t> input;
    input.reserve(pw.size() + salt.size());
    input.insert(input.end(),
        reinterpret_cast<const uint8_t*>(pw.data()),
        reinterpret_cast<const uint8_t*>(pw.data()) + pw.size());
    input.insert(input.end(), salt.begin(), salt.end());

    /* 2) 해시 계산 */
	Bytes64 out{};
    size_t  outLen = out.size();                  // =64
    int result = EVP_Q_digest(nullptr,            // 기본 libctx
	                          "SHA512",           // 알고리즘
	                          nullptr,            // prop-query
	                          input.data(), input.size(),
	                          out.data(), &outLen);

    if (result != 1 || outLen != out.size())
        throw std::runtime_error("EVP_Q_digest failed");

    return out;
}

void RegisterService::DoDbWork(SessionRef session, wstring email, Bytes64 pwHash, Bytes16 salt, OUT Protocol::RegisterResult& result, OUT int32& accountId)
{
    int32 rawAccountId = 0;
    int32 rawResult = 0;
    DBConnection* conn = GDBConnectionPool->Pop();
    
    {
        SP::Register accountRegister(*conn);

        // TODO sp의 outparams 다시 만들기
        // result에 대한 값을 다시 정해야한다. 지금은 기본값으로 없음.
        accountRegister.ParamIn_Email(email.c_str(), static_cast<int32>(email.size()));
        accountRegister.ParamIn_PwHash(pwHash.data(), 64);
        accountRegister.ParamIn_PwSalt(salt.data(), 16);
        accountRegister.ParamOut_AccId(rawAccountId);
        accountRegister.ParamOut_Result(rawResult);

        bool executeSuccess = accountRegister.Execute();
        if (executeSuccess)
        {
            // OUT 파라미터에서 결과 읽기
            cout << "✅ 회원가입 DB 처리 완료 - Result: " << rawResult << ", AccId: " << rawAccountId << endl;
        }
        else
        {
            cout << "❌ 회원가입 DB 실행 실패" << endl;
        }
    }
    GDBConnectionPool->Push(conn);

    // 그냥 accountId와 result만 OUT 매개변수로 처리
    accountId = rawAccountId;
    result = static_cast<Protocol::RegisterResult>(rawResult);
}

