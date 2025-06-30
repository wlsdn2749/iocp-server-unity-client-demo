using Packet;
using UnityEngine;
using UnityEngine.UI;
using TMPro;   

public class RegisterUI : MonoBehaviour
{
    [Header("UI refs")]
    [SerializeField] TMP_InputField emailInput;
    [SerializeField] TMP_InputField pwInput;
    [SerializeField] Button         registerBtn;

    void Awake()
    {
        registerBtn.onClick.AddListener(OnClickRegister);
    }

    void OnClickRegister()
    {
        string email    = emailInput.text.Trim();
        string pw       = pwInput.text;
        
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pw))
        {
            // 간단한 클라이언트 검증 – 메시지를 띄우든 경고 UI를 쓰든
            Debug.LogWarning("이메일/비밀번호를 입력하세요.");
            return;
        }
        
        // 패킷 생성 & 전송
        var pkt = new Protocol.C_REGISTER
        {
            Email = email,
            Pw = pw
        };

        var sendBuffer = ServerPacketManager.MakeSendBuffer(pkt);
        NetworkManager.Instance.Send(sendBuffer);
        
        // TODO 버튼 비활성화 (중복 방지)
    }
}
