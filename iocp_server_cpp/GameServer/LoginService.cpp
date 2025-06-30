#include "pch.h"
#include "LoginService.h"

#include <DBConnectionPool.h>
#include <openssl/evp.h>

#include "Genprocedures.h"

void LoginService::RequestLogin(SessionRef session, wstring_view email, wstring_view plainPw, int& result)
{
	Bytes16 salt = { };
	Bytes64 hash = { };
	DoDbWork(session, wstring(email), plainPw, OUT salt, OUT hash, result); // Salt와 Hash 획득

}

void LoginService::DoDbWork(SessionRef session, wstring email, wstring_view pw, Bytes16& salt, Bytes64& hash, int& result)
{

	DBConnection* conn = GDBConnectionPool->Pop();
	{
		SP::GetAccountHashSalt getAccountHashSalt(*conn);
		getAccountHashSalt.ParamIn_Email(email.c_str(), static_cast<int32>(email.size()));

		getAccountHashSalt.ColumnOut_PwHash(OUT hash);
		getAccountHashSalt.ColumnOut_PwSalt(OUT salt);

		getAccountHashSalt.Execute();
		if (getAccountHashSalt.Fetch()) // Fetch가 성공한 경우 = 데이터가 있는 경우
		{
			// 여기서 비교
			if (Validate(pw, salt, hash))
			{
				cout << "로그인 성공" << endl;
				result = Protocol::LoginResult::LOGIN_SUCCESS;
			}
			else
			{
				cout << "로그인 실패 // 비밀번호가 일치 하지 않습니다." << endl;
				result = Protocol::LoginResult::LOGIN_PW_MISMATCH;
			}
		}
		else // Fetch가 실패해 데이터가 없는 경우
		{
			cout << "로그인 실패 // Email이 등록되어 있지 않습니다" << endl;
			result = Protocol::LoginResult::LOGIN_EMAIL_NOT_FOUND;
			
		}


		
	}
	GDBConnectionPool->Push(conn);

}

bool LoginService::Validate(wstring_view pw, const Bytes16& dbSalt, const Bytes64& dbHash)
{
	// pw ‖ salt 를 한 버퍼에 붙이기 
	std::vector<uint8_t> input;
	input.reserve(pw.size() + dbSalt.size());
	input.insert(input.end(),
		reinterpret_cast<const uint8_t*>(pw.data()),
		reinterpret_cast<const uint8_t*>(pw.data()) + pw.size());
	input.insert(input.end(), dbSalt.begin(), dbSalt.end());

	Bytes64 out{};
	size_t  outLen = out.size();                  // =64
	int result = EVP_Q_digest(nullptr,            // 기본 libctx
		"SHA512",           // 알고리즘
		nullptr,            // prop-query
		input.data(), input.size(),
		out.data(), &outLen);

	if (result != 1 || outLen != out.size())
		throw std::runtime_error("EVP_Q_digest failed");

	if (out != dbHash)
	{
		return false;
	}

	return true;
}

