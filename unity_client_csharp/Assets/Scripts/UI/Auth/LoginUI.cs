using Packet;
using UnityEngine;
using UnityEngine.UI;
using TMPro;            

public class LoginUI : MonoBehaviour
{
    [Header("UI refs")]
    [SerializeField] TMP_InputField emailInput;
    [SerializeField] TMP_InputField pwInput;
    [SerializeField] Button        loginBtn;
    [SerializeField] Button        goRegisterBtn;
    [SerializeField] GameObject    registerPanel; // Inspector에 등록
    
    void Awake()
    {
        loginBtn.onClick.AddListener(OnClickLogin);
        goRegisterBtn.onClick.AddListener(SwitchToRegister);
    }
    
    void OnClickLogin()
    {
        string email = emailInput.text.Trim();
        string pw    = pwInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            // 간단한 클라이언트 검증 – 메시지를 띄우든 경고 UI를 쓰든
            Debug.LogWarning("이메일/비밀번호를 입력하세요.");
            return;
        }

        // 프로토 정의
        // 패킷 생성 & 전송
        var pkt = new Protocol.C_LOGIN
        {
            Email = email,
            Pw    = pw // 평문 
        };
        var sendBuffer = ServerPacketManager.MakeSendBuffer(pkt);
        NetworkManager.Instance.Send(sendBuffer); // 프로젝트의 전송 헬퍼 사용
    }
    
    void SwitchToRegister()
    {
        gameObject.SetActive(false);
        registerPanel.SetActive(true);
    }
    
    // TODO 버튼 비활성화 (중복 방지)
}
