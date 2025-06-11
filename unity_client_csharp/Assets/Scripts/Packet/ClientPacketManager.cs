// using ServerCore;
// using System;
// using System.Collections.Generic;
// using System.Diagnostics;
// using Google.Protobuf;
//
// public class PacketManager
// {
//     #region Singleton
//     static PacketManager _instance = new PacketManager();
//         
//     public static PacketManager Instance
//     {
//         get { return _instance; }
//     }
//     #endregion
//     
//     PacketManager()
//     {
//         Register();
//     }
//
//     Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>> _makeFunc = new Dictionary<ushort, Func<PacketSession, ArraySegment<byte>, IPacket>>();
//     Dictionary<ushort, Action<PacketSession, IPacket>> _handler = new Dictionary<ushort, Action<PacketSession, IPacket>>();
//
//     public void Register()
//     {
//       _makeFunc.Add((ushort)PacketID.S_BroadcastEnterGame, MakePacket<S_BroadcastEnterGame>);
//         _handler.Add((ushort)PacketID.S_BroadcastEnterGame, PacketHandler.S_BroadcastEnterGameHandler);
//
//       _makeFunc.Add((ushort)PacketID.S_BroadCastLeaveGame, MakePacket<S_BroadCastLeaveGame>);
//         _handler.Add((ushort)PacketID.S_BroadCastLeaveGame, PacketHandler.S_BroadCastLeaveGameHandler);
//
//       _makeFunc.Add((ushort)PacketID.S_PlayerList, MakePacket<S_PlayerList>);
//         _handler.Add((ushort)PacketID.S_PlayerList, PacketHandler.S_PlayerListHandler);
//
//       _makeFunc.Add((ushort)PacketID.S_BroadCastMove, MakePacket<S_BroadCastMove>);
//         _handler.Add((ushort)PacketID.S_BroadCastMove, PacketHandler.S_BroadCastMoveHandler);
//
//
//     }
//
//     public void OnRecvPacket(PacketSession session, ArraySegment<byte> buffer, Action<PacketSession, IPacket> OnRecvCallback = null)
//     {
//         ushort count = 0;
//         ushort size = BitConverter.ToUInt16(buffer.Array, buffer.Offset);
//         count += 2;
//         ushort id = BitConverter.ToUInt16(buffer.Array, buffer.Offset + 2);
//         count += 2;
//
//         switch (id)
//         {
//             case (ushort)PacketID.S_CHAT:
//                 {
//                     int protobufLen = size - 4;
//                     int protoOffset = buffer.Offset + 4;
//                     // Protocol 네임스페이스의 S_CHAT 바로 생성
//                     var pkt = Protocol.S_CHAT.Parser.ParseFrom(buffer.Array, protoOffset, protobufLen);
//                     S_CHATHandler(session, pkt);
//                     break;
//                 }
//             default:
//                 {
//                     // ...다른 패킷 분기
//                     Func<PacketSession, ArraySegment<byte>, IPacket> func = null;
//                     if (_makeFunc.TryGetValue(id, out func))
//                     {
//                         IPacket packet = func.Invoke(session, buffer);
//                         if (OnRecvCallback != null)
//                             OnRecvCallback.Invoke(session, packet);
//                         else
//                             HandlePacket(session, packet);
//                     }
//                     break;
//                 }
//         }
//
//     }
//
//     T MakePacket<T>(PacketSession session, ArraySegment<byte> buffer) where T : IPacket, new()
//     {
//         T pkt = new T();
//         pkt.Read(buffer);
//
//         return pkt;
//     }
//
//     public void HandlePacket(PacketSession session, IPacket packet)
//     {
//         Action<PacketSession, IPacket> action = null;
//         if(_handler.TryGetValue(packet.Protocol, out action))
//             action.Invoke(session, packet);
//     }
//     
//     public static void S_CHATHandler(PacketSession session, Protocol.S_CHAT pkt)
//     {
//         UnityEngine.Debug.Log($"[채팅] {pkt.PlayerId}: {pkt.Msg}");
//         // 채팅 UI 갱신 등 추가 작업
//     }
// }
