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
    public void Register(S_REGISTER packet)
    {
        var result = packet.Result;
        
        switch (result)
        {
            case RegisterResult.RegisterDuplicateEmail:
                Debug.LogWarning("📧 회원가입 실패: 이미 사용 중인 이메일입니다.");
                // 여기서 EmailInputField.Focus() 등 UI 처리
                return;

            case RegisterResult.RegisterServerError:
                Debug.LogError("🔑 회원가입 실패: 서버 내부 오류.");
                return;

            case RegisterResult.RegisterDefaultError:
                Debug.LogError("🔑 회원가입 실패: 기본 에러.");
                return;

            case RegisterResult.RegisterSuccess:
                Debug.Log("✅ 회원가입 성공!");
                //    여기서는 로그인 화면으로 이동 후, 사용자에게 로그인 유도
                UIManager.Instance.SetState(UIState.Login); 
                return;

            default:
                Debug.LogError($"⚠️ 알 수 없는 결과 코드: {(int)result}");
                return;
        }
    }
    public void Login(S_LOGIN packet)
    {
        // enum 형 따로 변수에 담아두면 보기 편함
        var result = packet.Result;

        switch (result)
        {
            case LoginResult.LoginEmailNotFound:
                Debug.LogWarning("🔑 로그인 실패: 가입되지 않은 이메일입니다.");
                return;

            case LoginResult.LoginPwMismatch:
                Debug.LogWarning("🔑 로그인 실패: 비밀번호 불일치.");
                return;

            case LoginResult.LoginServerError:
                Debug.LogError("🔑 로그인 실패: 서버 내부 오류.");
                return;
            
            case LoginResult.LoginDefaultError:
                Debug.LogError("로그인 실패: 기본 에러.");
                return;

            case LoginResult.LoginSuccess:
                Debug.Log("✅ 로그인 성공!");
                if (packet.Players.Count == 0)
                {
                    Debug.Log("👤 캐릭터가 없어서 생성 창으로 이동해야 합니다.");
                    // TODO: CharacterCreationUI.Show();
                    return; // 캐릭터 먼저 만들고 나가므로 아래 로직 스킵
                }
                break;

            default:
                Debug.LogError($"⚠️ 알 수 없는 결과 코드: {(int)result}");
                return; // 예측 못 한 코드면 입장 차단
        }

        // --- 성공 & 캐릭터 존재 → 게임 입장 패킷 전송 -----------------
        var enterGamePkt = new C_ENTER_GAME
        {
            PlayerIndex = 0 // 첫 번째 캐릭터 *고정*
        };

        var sendBuffer = ServerPacketManager.MakeSendBuffer(enterGamePkt);
        NetworkManager.Instance.Send(sendBuffer);
        UIManager.Instance.SetState(UIState.Game); // ★ 로비/게임 화면으로 전환
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
            // _myPlayer.transform.position = new Vector3(packet.PosX, packet.PosY, packet.PosZ);
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
