using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
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
        [Header("Appearance")]
        [SerializeField] private int _fontSize = 14;
        [SerializeField] float _refreshSec = 0.1f; // 10 Hz (필요에 따라 조절)
        
        private WaitForSeconds _yieldWait;
        private StringBuilder _sb = new StringBuilder(8192);
        private bool _dirty; // 이번 프레임에 추가 메시지가 있었는가?
        
        const int _maxChars = 6000;   
        const int _trimTarget = 2000; // 자를 때 남길 문자 수

        void OnEnable()
        {
            _yieldWait = new WaitForSeconds(_refreshSec);
            StartCoroutine(RefreshLoop());
        }
        IEnumerator RefreshLoop()
        {
            while (true)
            {
                if (_dirty) // 새 메시지가 쌓였을 때만
                {
                    _dirty = false;
                    _chatContentText.SetText(_sb);
                    _scrollRect.verticalNormalizedPosition = 0f;
                }
                yield return _yieldWait; // ➜ 0.1 초 휴식
            }
        }
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
            string time = DateTime.Now.ToString("HH:mm");
            if (_sb.Length > 0) _sb.Append('\n'); // 줄바꿈
            _sb.Append($"[{time}] {sender} : {message}");

            // 1) 글자 수가 한계치 넘으면 앞부분 삭제
            if (_sb.Length > _maxChars)
                _sb.Remove(0, _sb.Length - _trimTarget);
            
            _dirty = true; // LateUpdate에서 한 번에 처리
        }
    }
} 