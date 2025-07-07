using TMPro;
using UnityEngine;

[RequireComponent(typeof(RectTransform))]
public class NameTagUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _label;
    private bool _showRtt;
    private ulong _id;
    private Transform _target;    
    private Vector3 _offset;
    private Camera _cam;

    /* 초기화 */
    public void Init(Transform t, ulong id, bool showRtt)
    {
        transform.localScale = Vector3.one;
        _target  = t;
        _offset = new Vector3(0, 1.3f, 0);
        _id      = id;
        _showRtt = showRtt;
        _cam = Camera.main;
        _label.SetText(showRtt ? $"{id} <size=70%>0 ms</size>" // 자기 자신
            : $"{id}");                                         // 남
    }

    /* RTT 갱신 – 자기 자신만 반응 */
    public void UpdateRtt(double rtt)
    {
        if (!_showRtt) return; // 다른 플레이어는 무시
        _label.SetText($"{_id}\n<size=70%>{rtt} ms</size>");
    }
    
    void LateUpdate()
    {
        if (!_target) return;
        var scr = _cam.WorldToScreenPoint(_target.position + _offset);
        transform.position = scr;
        gameObject.SetActive(scr.z > 0); // 카메라 앞쪽에만 표시
    }
}