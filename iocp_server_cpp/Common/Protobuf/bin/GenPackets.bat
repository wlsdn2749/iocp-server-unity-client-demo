pushd %~dp0

REM Proto 파일들 생성
protoc.exe -I=./ --cpp_out=./ ./Protocol.proto
protoc.exe -I=./ --cpp_out=./ ./Struct.proto
protoc.exe -I=./ --cpp_out=./ ./Enum.proto

protoc.exe -I=./ --csharp_out=./ ./Protocol.proto
protoc.exe -I=./ --csharp_out=./ ./Struct.proto
protoc.exe -I=./ --csharp_out=./ ./Enum.proto

REM 자동화된 Handler 생성
GenPackets.exe --path=./Protocol.proto --output=ClientPacketHandler --recv=C_ --send=S_
GenPackets.exe --path=./Protocol.proto --output=ServerPacketHandler --recv=S_ --send=C_
GenPackets.exe --path=./Protocol.proto --output=ServerPacketManager --ext=cs --template=PacketManager.cs --recv=S_ --send=C_

IF ERRORLEVEL 1 PAUSE

REM 각자 위치에 옮겨주기

REM IOCP C++ GameServer
XCOPY /Y Enum.pb.h "../../../GameServer"
XCOPY /Y Enum.pb.cc "../../../GameServer"
XCOPY /Y Struct.pb.h "../../../GameServer"
XCOPY /Y Struct.pb.cc "../../../GameServer"
XCOPY /Y Protocol.pb.h "../../../GameServer"
XCOPY /Y Protocol.pb.cc "../../../GameServer"
XCOPY /Y ClientPacketHandler.h "../../../GameServer"

REM C++ DummyClient
XCOPY /Y Enum.pb.h "../../../DummyClient"
XCOPY /Y Enum.pb.cc "../../../DummyClient"
XCOPY /Y Struct.pb.h "../../../DummyClient"
XCOPY /Y Struct.pb.cc "../../../DummyClient"
XCOPY /Y Protocol.pb.h "../../../DummyClient"
XCOPY /Y Protocol.pb.cc "../../../DummyClient"
XCOPY /Y ServerPacketHandler.h "../../../DummyClient"

REM C# Unity Client
XCOPY /Y Enum.cs "../../../../unity_client_csharp/Assets/Scripts/Protocol"
XCOPY /Y Struct.cs "../../../../unity_client_csharp/Assets/Scripts/Protocol"
XCOPY /Y Protocol.cs "../../../../unity_client_csharp/Assets/Scripts/Protocol"

XCOPY /Y ServerPacketManager.cs "../../../../unity_client_csharp/Assets/Scripts/Packet"

REM C# DummyClient
XCOPY /Y Enum.cs "../../../DummyClientCS/Protocol"
XCOPY /Y Struct.cs "../../../DummyClientCS/Protocol"
XCOPY /Y Protocol.cs "../../../DummyClientCS/Protocol"

XCOPY /Y ServerPacketManager.cs "../../../DummyClientCS/Packet"

REM DEL /Q /F *.pb.h
REM DEL /Q /F *.pb.cc
REM DEL /Q /F *.h
REM DEL /Q /F *.cs

PAUSE