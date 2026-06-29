using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 보스 전용 하단 네임드 체력바. BossGate 가 전투 시작/종료 시 Show/Hide 호출.
/// EnemyHealth 에 OnHealthChanged 가 없어 OnDamaged 구독 + Current/Max 읽기로 갱신.
/// </summary>
public class BossHealthBar : MonoBehaviour
{
    [Header("UI")]
    [Tooltip("토글되는 바 루트 (기본 비활성)")]
    [SerializeField] private GameObject _root;
    [Tooltip("체력 채움 (Image - Type: Filled)")]
    [SerializeField] private Image _fill;
    [SerializeField] private TextMeshProUGUI _nameText;

    private EnemyHealth _health;

    private void Awake()
    {
        if (_root != null) _root.SetActive(false);
    }

    /// <summary>전투 시작: 보스 HP 구독 + 바 등장.</summary>
    public void Show(EnemyHealth health, string bossName)
    {
        _health = health;
        if (_health != null) _health.OnDamaged += Refresh;

        if (_nameText != null) _nameText.text = bossName;
        Refresh();
        if (_root != null) _root.SetActive(true);
    }

    /// <summary>전투 종료(사망/재도전): 구독 해제 + 바 숨김.</summary>
    public void Hide()
    {
        if (_health != null) _health.OnDamaged -= Refresh;
        _health = null;
        if (_root != null) _root.SetActive(false);
    }

    private void Refresh()
    {
        if (_health == null || _fill == null) return;
        _fill.fillAmount = _health.MaxHealth > 0
            ? (float)_health.CurrentHealth / _health.MaxHealth
            : 0f;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDamaged -= Refresh;
    }
}