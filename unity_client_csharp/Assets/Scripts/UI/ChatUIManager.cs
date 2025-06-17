using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace UI
{
    public class ChatUIManager : MonoBehaviour
    {
        public static ChatUIManager Instance { get; private set; }

        [Header("UI References")]
        [Tooltip("스크롤 뷰 컴포넌트 (Vertical)")]
        [SerializeField] private ScrollRect _scrollRect;
        [Tooltip("채팅 내용을 표시할 TMP_Text 컴포넌트 (Content 영역)")]
        [SerializeField] private TMP_Text _chatContentText;
        [Tooltip("저장할 최대 메시지 수")]
        [SerializeField] private int _maxMessages = 50;
        [Header("Appearance")]
        [SerializeField] private int _fontSize = 14;

        private readonly List<string> _messages = new List<string>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            if (_chatContentText != null)
            {
                _chatContentText.fontSize = _fontSize;
                _chatContentText.alignment = TextAlignmentOptions.BottomLeft;
            }
        }

        /// <summary>
        /// 서버 브로드캐스트 메시지를 UI에 추가한다.
        /// </summary>
        public void AddMessage(string sender, string message)
        {
            if (_chatContentText == null) return;

            string time = DateTime.Now.ToString("HH:mm");
            string formatted = $"[{time}] {sender} : {message}";

            _messages.Add(formatted);
            if (_messages.Count > _maxMessages)
                _messages.RemoveAt(0);

            // UI 업데이트
            _chatContentText.text = string.Join("\n", _messages);

            // 레이아웃 강제 갱신
            LayoutRebuilder.ForceRebuildLayoutImmediate(_chatContentText.rectTransform);

            // 맨 아래로 스크롤
            Canvas.ForceUpdateCanvases();
            if (_scrollRect != null)
            {
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }
    }
} 