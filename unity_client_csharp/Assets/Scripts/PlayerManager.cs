using System;
using System.Collections.Generic;
using Protocol;
using UnityEngine;
using Packet;
using Object = UnityEngine.Object;

public class PlayerManager : MonoBehaviour
{
    [Header("Prefabs / Parents")]
    [SerializeField] private GameObject playerPrefab;    // "Resources/Player" 대신 인스펙터 지정
    [SerializeField] private NameTagUI   nameTagPrefab;  // UI 프리팹 (앞서 만든 것)
    [SerializeField] private RectTransform nameTagLayer; // Canvas 하위 빈 오브젝트
    
    [SerializeField] private Material matMyPlayer;  // 흰색 + 아웃라인
    [SerializeField] private Material matPlayer; // 초록
    [SerializeField] private Material matDummy; // 회색·투명 60 %
    
    public MyPlayer MyPlayer { get; private set; }
    private Dictionary<ulong, Player> _players = new Dictionary<ulong, Player>();

    public static PlayerManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public int TotalPlayerCount => (MyPlayer != null ? 1 : 0) + _players.Count;
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

        foreach (Protocol.Player p in packet.Players)
        {
            SpawnPlayer(p, packet.MyPlayerId);
        }
    }

    /* ---------------------------------------------------------------------- */
    /*  SpawnPlayer : 1명 생성 & NameTag 연결                               */
    /* ---------------------------------------------------------------------- */
    private void SpawnPlayer(Protocol.Player p, ulong myId)
    {
        // ┌─── ① 플레이어 GameObject 생성 ──────────────────────────────────┐
        var go = Object.Instantiate(playerPrefab);
        go.transform.position = new Vector3(p.PosX, p.PosY, p.PosZ);

        Debug.Log($"Position: {go.transform.position}");
        /* 1) 플레이어 타입 판단 --------------------------------------- */
        bool isSelf  = p.Id == myId;
        bool isDummy = p.PlayerType == PlayerType.Dummy;
        Material mat = isSelf    ? matMyPlayer
            : isDummy   ? matDummy
            :             matPlayer;

        /* 2) 머티리얼 적용 (공유 머티리얼 유지) ------------------------- */
        ApplySharedMaterial(go, mat);
        
        // ┌─── ② 타입 구분 (자신 / 타인) ───────────────────────────────────┐
        Player baseComp;
        if (isSelf)
        {
            var me = go.AddComponent<MyPlayer>();
            me.PlayerId = p.Id;
            MyPlayer = me;
            baseComp = me;
        }
        else
        {
            var other = go.AddComponent<Player>();
            other.PlayerId = p.Id;
            _players.Add(p.Id, other);
            baseComp = other;
        }

        // ┌─── ③ NameTag UI 인스턴스화 ─────────────────────────────────────┐
        var tag = Object.Instantiate(nameTagPrefab, nameTagLayer, false);
        tag.Init(baseComp.transform, p.Id, isSelf);
        baseComp.AttachNameTag(tag);
    }
    
    
    /// <summary>
    /// “내 캐릭터” 여부를 따질 필요 없이
    /// **다른 플레이어**만 생성할 때 사용하는 헬퍼.
    /// </summary>
    private void SpawnRemotePlayer(Protocol.Player p)
    {
        // ┌─── ① 오브젝트 배치 ───────────────────────────────────────────────┐
        var go = Instantiate(playerPrefab);
        go.transform.position = new Vector3(p.PosX, p.PosY, p.PosZ);

        // ┌─── ② 머티리얼 지정 (Dummy / 일반) ────────────────────────────────┐
        bool isDummy = p.PlayerType == PlayerType.Dummy;
        Material mat = isDummy ? matDummy : matPlayer;
        ApplySharedMaterial(go, mat);

        // ┌─── ③ Player 컴포넌트 등록 - 모두 ‘타인’ 취급 ─────────────────────┐
        var other = go.AddComponent<Player>();
        other.PlayerId = p.Id;
        _players.Add(p.Id, other);

        // ┌─── ④ NameTag UI 생성 ────────────────────────────────────────────┐
        var tag = Instantiate(nameTagPrefab, nameTagLayer, false);
        tag.Init(other.transform, p.Id, /*isSelf:*/ false);
        other.AttachNameTag(tag);
    }
    public void Move(S_BROADCAST_MOVE packet)
    {
        if (MyPlayer.PlayerId == packet.PlayerId)
        {
            // MyPlayer.transform.position = new Vector3(packet.PosX, packet.PosY, packet.PosZ);
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
    
    private static void ApplySharedMaterial(GameObject go, Material sharedMat)
    {
        if (go.TryGetComponent(out MeshRenderer mr))
        {
            mr.sharedMaterial = sharedMat; // ▶ 공유 자원, 런타임에 복제 X
            mr.enabled        = true;      // 혹시 비활성화되어 있다면
            // Instancing 옵션은 머티리얼 쪽에서 이미 On
        }
    }
    //
    public void EnterGame(S_BROADCAST_ENTER_GAME packet)
    {
        Debug.Log("EnterGameBroadCast를 받아, EnterGame호출");
        Protocol.Player p = packet.Player;

        if (p.Id == MyPlayer.PlayerId) return;

        SpawnRemotePlayer(p); // 0은 더미값
    }
    
    public void LeaveGame(S_BROADCAST_LEAVE_GAME packet)
    {
        if (MyPlayer.PlayerId == packet.PlayerId)
        {
            GameObject.Destroy(MyPlayer.gameObject);
            MyPlayer = null;
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
