using System;
using UnityEngine;

/// <summary>
/// 플레이어 최종 스탯 계산. 기본값(PlayerStatsConfig) + 장비 보정(EquipmentManager)을 합산.
/// (레벨업 없이 장비로만 변하는 스탯 - 범위 축소 설계)
/// 
/// 공격력: 공격 시 콤보 데미지(동작 위력)에 더해짐 (PlayerAttacker 참조)
/// 방어력: 피격 시 받는 데미지 감소 (PlayerHealth 참조)
/// 장비 변경 시 OnStatsChanged 발행 (UI 갱신).
/// </summary>
public class PlayerStats : MonoBehaviour
{
    [SerializeField] private PlayerStatsConfig _config;
    [SerializeField] private EquipmentManager _equipment;

    /// <summary>스탯 변경 시 발행 (UI 갱신).</summary>
    public event Action OnStatsChanged;

    /// <summary>최종 공격력 = 기본(Config) + 장비 보정.</summary>
    public int Attack
    {
        get
        {
            int baseAtk = _config != null ? _config.BaseAttack : 0;
            int equip = _equipment != null ? _equipment.GetTotalAttackBonus() : 0;
            return baseAtk + equip;
        }
    }

    /// <summary>최종 방어력 = 기본(Config) + 장비 보정.</summary>
    public int Defense
    {
        get
        {
            int baseDef = _config != null ? _config.BaseDefense : 0;
            int equip = _equipment != null ? _equipment.GetTotalDefenseBonus() : 0;
            return baseDef + equip;
        }
    }

    private void Awake()
    {
        if (_equipment == null) _equipment = GetComponent<EquipmentManager>();
        // _config 는 Inspector 에서 할당 (PlayerStatsConfig 에셋)
    }

    private void OnEnable()
    {
        if (_equipment != null) _equipment.OnEquipmentChanged += HandleEquipmentChanged;
    }

    private void OnDisable()
    {
        if (_equipment != null) _equipment.OnEquipmentChanged -= HandleEquipmentChanged;
    }

    private void HandleEquipmentChanged()
    {
        OnStatsChanged?.Invoke();  // 장비 변경 → 스탯 변경 알림
    }
}