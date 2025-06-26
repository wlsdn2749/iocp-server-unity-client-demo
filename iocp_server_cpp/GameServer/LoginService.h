#pragma once

/*-------------------
	LoginService
-------------------*/

class LoginService : public std::enable_shared_from_this<LoginService>
{
public:
	// 패킷 핸들러에서 호출하는 진입점
	void RequestLogin(SessionRef session, wstring_view email, wstring_view plainPw);

private:
	// DB 스레드로 넘겨서, 프로시저를 호출하게 함
	void DoDbWork(SessionRef session, wstring email, BYTE* pwHash);

	// 네트워크/메인 스레드 -> Client로 결과 응답
	void Finish(SessionRef session, uint8 result, int32 accountId);

	// pw 해시 계산 (pw + salt) -> SHA256  
	void CalcPwHash(wstring_view pw) const;
};

