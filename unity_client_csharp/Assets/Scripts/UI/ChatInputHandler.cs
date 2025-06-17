using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using Packet;
using Protocol;
using UnityEngine.EventSystems;

namespace UI
{
    public class ChatInputHandler : MonoBehaviour
    {
        [Header("UI References")]
        [SerializeField] private TMP_InputField _inputField;
        [SerializeField] private Button _sendButton;

        private bool _isFocused;

        private void Awake()
        {
            if (_inputField == null)
                _inputField = GetComponent<TMP_InputField>();

            if (_inputField != null)
            {
                // 엔터로 전송
                _inputField.onEndEdit.AddListener(OnEndEdit);
                // 입력 가능 & 커서 설정
                if (_inputField != null)
                {
                    _inputField.interactable = true;
                    _inputField.readOnly = false;
                    _inputField.customCaretColor = true;
                    _inputField.caretColor = Color.white;
                    _inputField.caretWidth = 2;
                    _inputField.caretBlinkRate = 0.7f;
                }
            }
            else
            {
                Debug.LogError("ChatInputHandler: TMP_InputField component가 없습니다.");
            }

            if (_sendButton != null)
            {
                _sendButton.onClick.AddListener(SendChat);
            }
        }

        private void OnDestroy()
        {
            if (_inputField != null)
            {
                _inputField.onEndEdit.RemoveListener(OnEndEdit);
            }
            if (_sendButton != null)
            {
                _sendButton.onClick.RemoveListener(SendChat);
            }
        }

        private void OnEndEdit(string _)
        {
            // InputField에서 Enter 누르면 채팅 전송 & 포커스 해제
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                SendChat();
                ReleaseFocus();
            }
            else
            {
                ReleaseFocus();
            }
        }

        private void SendChat()
        {
            if (_inputField == null) return;

            string msg = _inputField.text.Trim();
            if (string.IsNullOrEmpty(msg)) return;

            C_CHAT chatPacket = new C_CHAT { Msg = msg };
            var sendBuffer = ServerPacketManager.MakeSendBuffer(chatPacket);
            NetworkManager.Instance.Send(sendBuffer);

            _inputField.text = string.Empty;
            _inputField.MoveTextEnd(false);
        }

        private void FocusInputField()
        {
            if (_inputField == null) return;
            _isFocused = true;
            _inputField.interactable = true; // 보장
            _inputField.ActivateInputField();
            _inputField.Select();
        }

        private void ReleaseFocus()
        {
            if (_inputField == null) return;
            _isFocused = false;
            EventSystem.current?.SetSelectedGameObject(null);
        }

        private void Update()
        {
            // Enter 키로 InputField 포커스 토글 (채팅 시작)
            if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            {
                if (!_isFocused)
                {
                    FocusInputField();
                }
            }
        }
    }
} 