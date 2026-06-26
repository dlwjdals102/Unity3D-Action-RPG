using UnityEngine;
using TMPro;

/// <summary>
/// 영혼 보유량 HUD. PlayerSouls 를 구독해 화면에 표시한다.
/// 적 처치/상점 구매 시 자동 갱신 (이벤트 기반).
/// </summary>
public class SoulsHUD : MonoBehaviour
{
    [SerializeField] private PlayerSouls _souls;

    [Tooltip("영혼 보유량 텍스트")]
    [SerializeField] private TextMeshProUGUI _soulsText;

    private void OnEnable()
    {
        if (_souls != null)
        {
            _souls.OnSoulsChanged += UpdateText;
            UpdateText(_souls.Souls);  // 초기값 즉시 표시
        }
    }

    private void OnDisable()
    {
        if (_souls != null) _souls.OnSoulsChanged -= UpdateText;
    }

    private void UpdateText(int souls)
    {
        if (_soulsText != null) _soulsText.text = $"Soul: {souls}";
    }
}