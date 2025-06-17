using System.Collections.Generic;
using Protocol;
using UnityEngine;
using Packet;

public class PlayerManager
{
    private MyPlayer _myPlayer;
    private Dictionary<ulong, Player> _players = new Dictionary<ulong, Player>();

    public static PlayerManager Instance { get; } = new PlayerManager();
    
    public int TotalPlayerCount => (_myPlayer != null ? 1 : 0) + _players.Count;
    
    public void Login(S_LOGIN packet)
    {
        if (packet.Success == false)
            return;

        if (packet.Players.Count == 0)
        {
            // 캐릭터 생성창으로 이동
        }

        // 입장 UI 버튼 눌러서 게임 입장
        Protocol.C_ENTER_GAME enterGamePkt = new C_ENTER_GAME()
        {
            PlayerIndex = 0, // 첫번째 캐릭터로 입장
        };
        var sendBuffer = ServerPacketManager.MakeSendBuffer(enterGamePkt);
        NetworkManager.Instance.Send(sendBuffer);
        return;
    }
    public void Add(S_PLAYERLIST packet)
    {
        Debug.Log("PlayerList를 받아 Add 요청 받음");
        Object obj = Resources.Load("Player");
        
        foreach (Protocol.Player p in packet.Players)
        {
            GameObject go = Object.Instantiate(obj) as GameObject;
            if(packet.MyPlayerId == p.Id) // 자기 자신인 경우.
            {
                MyPlayer myPlayer = go.AddComponent<MyPlayer>();
                myPlayer.PlayerId = p.Id;
                myPlayer.transform.position = new Vector3(p.PosX, p.PosY, p.PosZ);
                _myPlayer = myPlayer;
            }
            else
            {
                Player player = go.AddComponent<Player>();
                player.PlayerId = p.Id;
                player.transform.position = new Vector3(p.PosX, p.PosY, p.PosZ);
                _players.Add(p.Id, player);
            }
        }
    }
    public void Move(S_BROADCAST_MOVE packet)
    {
        if (_myPlayer.PlayerId == packet.PlayerId)
        {
            _myPlayer.transform.position = new Vector3(packet.PosX, packet.PosY, packet.PosZ);
        }
        else
        {
            Player player = null;
            // _players에 packet.playerId라는 Key가 있으면 True반환하고 player에 그 value를 넘김
            // False인 경우 Value의 Default값이 Player에 넘어감
            if (_players.TryGetValue(packet.PlayerId, out player))
            {
                //Debug.Log($"Pos x,y,z = ({packet.PosX}, {packet.PosY}, {packet.PosZ})");
                player.OnMovePacket(packet);
            }
        }
    }
    //
    public void EnterGame(S_BROADCAST_ENTER_GAME packet)
    {
        Debug.Log("EnterGameBroadCast를 받아, EnterGame호출");
        if (packet.PlayerId == _myPlayer.PlayerId)
            return;
        
        Object obj = Resources.Load("Player");
        GameObject go = Object.Instantiate(obj) as GameObject;
        
        Player player = go.AddComponent<Player>();
        player.transform.position = new Vector3(packet.PosX, packet.PosY, packet.PosZ);
        _players.Add(packet.PlayerId, player);
    }
    
    public void LeaveGame(S_BROADCAST_LEAVE_GAME packet)
    {
        if (_myPlayer.PlayerId == packet.PlayerId)
        {
            GameObject.Destroy(_myPlayer.gameObject);
            _myPlayer = null;
        }
        else
        {
            Player player = null;
            if (_players.TryGetValue(packet.PlayerId, out player))
            {
                GameObject.Destroy(player.gameObject);
                _players.Remove(packet.PlayerId);
            }
        }
    }
}
