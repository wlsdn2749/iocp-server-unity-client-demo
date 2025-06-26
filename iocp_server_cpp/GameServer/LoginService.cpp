#include "pch.h"
#include "LoginService.h"

void LoginService::RequestLogin(SessionRef session, wstring_view email, wstring_view plainPw)
{
	auto self = shared_from_this(); // 람다 내부에서 수명 보장
	/*BYTE* hash = CalcPwHash(plainPw);*/

}

void LoginService::DoDbWork(SessionRef session, wstring email, BYTE* pwHash)
{
}

void LoginService::Finish(SessionRef session, uint8 result, int32 accountId)
{
}

void LoginService::CalcPwHash(wstring_view pw) const
{
	// 
}
