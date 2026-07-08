using UnityEngine;
using TMPro;

/// <summary>
/// 스탯 구역 UI. PlayerStats 의 공격력/방어력을 표시한다.
/// 장비 변경(OnStatsChanged) 시 자동 갱신.
/// </summary>
public class StatsUI : MonoBehaviour
{
    [SerializeField] private PlayerStats _stats;

    [Tooltip("공격력 텍스트")]
    [SerializeField] private TextMeshProUGUI _attackText;

    [Tooltip("무기 데미지 텍스트")]
    [SerializeField] private TextMeshProUGUI _weaponDamageText;

    [Tooltip("방어력 텍스트")]
    [SerializeField] private TextMeshProUGUI _defenseText;

    private void OnEnable()
    {
        if (_stats != null) _stats.OnStatsChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (_stats != null) _stats.OnStatsChanged -= Refresh;
    }

    private void Refresh()
    {
        if (_stats == null) return;

        if (_attackText != null) _attackText.text = $"Attack: {_stats.Attack}";
        if (_weaponDamageText != null) _weaponDamageText.text = $"Weapon: {_stats.WeaponDamage}";
        if (_defenseText != null) _defenseText.text = $"Defense: {_stats.Defense}";

    }
}