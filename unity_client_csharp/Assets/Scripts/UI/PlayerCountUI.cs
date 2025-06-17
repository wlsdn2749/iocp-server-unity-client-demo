using UnityEngine;
using TMPro;

namespace UI
{
    public class PlayerCountUI : MonoBehaviour
    {
        [SerializeField] private TMP_Text _countText;
        [SerializeField] private string _format = "Players : {0}";
        [SerializeField] private float _updateInterval = 0.5f;

        private float _elapsed;

        private void Awake()
        {
            if (_countText == null)
                _countText = GetComponent<TMP_Text>();
        }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;
            if (_elapsed < _updateInterval) return;
            _elapsed = 0f;

            int count = PlayerManager.Instance.TotalPlayerCount;
            if (_countText != null)
                _countText.text = string.Format(_format, count);
        }
    }
} 