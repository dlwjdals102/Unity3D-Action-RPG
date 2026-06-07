using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 플레이어 장비 착용 관리. 슬롯별로 착용한 장비(EquipmentData)를 보관한다.
/// 착용(Equip)/해제(Unequip) 시 인벤토리와 주고받는다 (방식 A):
///   - 착용: 인벤토리에서 제거 → 슬롯에 등록 (기존 착용품은 인벤토리로 반환)
///   - 해제: 슬롯 비우고 → 인벤토리로 반환
/// 변경 시 OnEquipmentChanged 발행 → UI/스탯 갱신 (이벤트 기반).
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;

    // 슬롯별 착용 장비 (없으면 키 없음 또는 null)
    private readonly Dictionary<EquipmentSlot, EquipmentData> _equipped =
        new Dictionary<EquipmentSlot, EquipmentData>();

    /// <summary>장비 변경 시 발행 (UI 갱신, 스탯 재계산).</summary>
    public event Action OnEquipmentChanged;

    private void Awake()
    {
        if (_inventory == null) _inventory = GetComponent<Inventory>();
    }

    /// <summary>해당 슬롯에 착용된 장비 (없으면 null).</summary>
    public EquipmentData GetEquipped(EquipmentSlot slot)
    {
        return _equipped.TryGetValue(slot, out var item) ? item : null;
    }

    /// <summary>
    /// 장비 착용. 인벤토리에서 해당 장비를 제거하고 슬롯에 등록한다.
    /// 같은 슬롯에 이미 착용품이 있으면 인벤토리로 반환한다.
    /// </summary>
    public void Equip(EquipmentData equipment)
    {
        if (equipment == null || _inventory == null) return;

        EquipmentSlot slot = equipment.Slot;

        // 1. 인벤토리에서 착용할 장비 제거
        _inventory.RemoveItem(equipment, 1);

        // 2. 기존 착용품이 있으면 인벤토리로 반환
        if (_equipped.TryGetValue(slot, out var previous) && previous != null)
        {
            _inventory.AddItem(previous, 1);
        }

        // 3. 새 장비 착용
        _equipped[slot] = equipment;

        OnEquipmentChanged?.Invoke();
    }

    /// <summary>
    /// 장비 해제. 슬롯을 비우고 해당 장비를 인벤토리로 반환한다.
    /// </summary>
    public void Unequip(EquipmentSlot slot)
    {
        if (!_equipped.TryGetValue(slot, out var item) || item == null) return;

        // 슬롯 비우고 인벤토리로 반환
        _equipped[slot] = null;
        _inventory.AddItem(item, 1);

        OnEquipmentChanged?.Invoke();
    }

    /// <summary>모든 착용 장비의 공격력 보정 합 ([5] 스탯에서 사용).</summary>
    public int GetTotalAttackBonus()
    {
        int total = 0;
        foreach (var item in _equipped.Values)
        {
            if (item != null) total += item.AttackBonus;
        }
        return total;
    }

    /// <summary>모든 착용 장비의 방어력 보정 합 ([5] 스탯에서 사용).</summary>
    public int GetTotalDefenseBonus()
    {
        int total = 0;
        foreach (var item in _equipped.Values)
        {
            if (item != null) total += item.DefenseBonus;
        }
        return total;
    }
}