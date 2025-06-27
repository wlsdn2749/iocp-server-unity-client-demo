#pragma once

/*-------------------
	LoginService
-------------------*/
using Bytes16 = Array<BYTE, 16>;
using Bytes64 = Array<BYTE, 64>;

class LoginService
{
public:
	// Singleton LoginService
	static LoginService& Instance()
	{
		static LoginService s;
		return s;
	}

public:
	// 패킷 핸들러에서 호출하는 진입점
	void RequestLogin(SessionRef session, wstring_view email, wstring_view plainPw, int& result);

private:
	// DB 스레드로 넘겨서, 프로시저를 호출하게 함
	void DoDbWork(SessionRef session, wstring email, wstring_view plainPw, Bytes16& salt, Bytes64& hash, int& result);

	// 입력 받은 pw+salt를 SHA512로 변환한게 같은지 확인
	bool Validate(wstring_view pw, const Bytes16& salt, const Bytes64& hash);
};

