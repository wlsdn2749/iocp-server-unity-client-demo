#pragma once
#include "JobQueue.h"
using Bytes16 = Array<BYTE, 16>;
using Bytes64 = Array<BYTE, 64>;

class RegisterService
						
{
public:
    // Singleton Register Service
    static RegisterService& Instance()
    {
	    static RegisterService s;
        return s;
    }
    void RequestRegister(SessionRef session, wstring_view email, wstring_view plainPw,
        OUT Protocol::RegisterResult& result, OUT int32& accountId);

private:
    Bytes16 MakeSalt() const;
    Bytes64 CalcPwHash(wstring_view pw, const Bytes16& salt) const;

    void DoDbWork(SessionRef session, wstring email, Bytes64 pwHash, Bytes16 salt, OUT Protocol::RegisterResult& result, OUT int32& accountId);
};