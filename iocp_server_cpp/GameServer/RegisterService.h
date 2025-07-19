#pragma once
#include "JobQueue.h"
using Bytes16 = Array<BYTE, 16>;
using Bytes64 = Array<BYTE, 64>;

class RegisterService			
{
public:
    static RegisterService& Instance()
    {
	    static RegisterService s;
        return s;
    }
    // 패킷 핸들러에서 진입하는 진입점 : Handle_C_REGISTER
    void RequestRegister(SessionRef session, wstring_view email, wstring_view plainPw,
        OUT Protocol::RegisterResult& result, OUT int32& accountId);

private:
    // 랜덤 16byte Salt를 반환한다.
    Bytes16 MakeSalt() const;
    
    // pw와 salt를 더해 SHA-512 알고리즘으로 만든 해쉬를 반환한다.
    Bytes64 CalcPwHash(wstring_view pw, const Bytes16& salt) const;

    // DB작업
    void DoDbWork(SessionRef session, wstring email, Bytes64 pwHash, Bytes16 salt, OUT Protocol::RegisterResult& result, OUT int32& accountId);
};