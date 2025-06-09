#pragma once
#include "IocpCore.h"
#include "NetAddress.h"

/*--------------
	Listener
--------------*/
class AcceptEvent;
class ServerService;

class Listener : public IocpObject
{
public:
	Listener() = default;
	~Listener();

public:
	/*외부에서 사용*/
	bool StartAccept(ServerServiceRef service); // Listener가 문지기 역할을 해라
	void CloseSocket();

public:
	/* 인터페이스 구현 */
	virtual HANDLE GetHandle() override;
	virtual void Dispatch(class IocpEvent* iocpEvent, int32 numOfBytes = 0) override;


protected:
	SOCKET _socket = INVALID_SOCKET; // Listener 소켓
	Vector<AcceptEvent*> _acceptEvents;
	ServerServiceRef _service; // Listener가 속한 서비스	
	

private:
	/* 수신 관련 */
	void RegisterAccept(AcceptEvent* acceptEvent);
	void ProcessAccept(AcceptEvent* acceptEvent);
};

