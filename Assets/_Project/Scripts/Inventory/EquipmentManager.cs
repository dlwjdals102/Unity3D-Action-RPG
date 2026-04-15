using System;
using UnityEngine;

/// <summary>
/// 장비 관리자. 무기/방어구/악세서리 장착 슬롯을 관리하고
/// 장비 스탯을 PlayerStats에 반영합니다.
/// 
/// [장비 슬롯]
/// Weapon, Armor, Accessory — 각 1칸
/// </summary>
public class EquipmentManager : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  장비 슬롯
    // ════════════════════════════════════════════════════

    private ItemData _weapon;
    private ItemData _armor;
    private ItemData _accessory;

    public ItemData Weapon => _weapon;
    public ItemData Armor => _armor;
    public ItemData Accessory => _accessory;

    // ── 이벤트 ──
    /// <summary>장비 변경 시 (슬롯 타입)</summary>
    public event Action<Define.ItemType> OnEquipmentChanged;

    // ── 참조 ──
    private InventorySystem _inventory;
    private PlayerStats _stats;

    // ── 보너스 스탯 캐시 ──
    public float BonusAttack { get; private set; }
    public float BonusDefense { get; private set; }
    public float BonusMaxHp { get; private set; }

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        _inventory = GetComponent<InventorySystem>();
        _stats = GetComponent<PlayerStats>();
    }

    // ════════════════════════════════════════════════════
    //  장착
    // ════════════════════════════════════════════════════

    /// <summary>
    /// 아이템을 장착합니다. 기존 장비는 인벤토리로 돌아갑니다.
    /// </summary>
    public bool Equip(ItemData item, int inventorySlotIndex)
    {
        if (item == null || !item.IsEquipment) return false;

        ItemData previousEquip = GetEquippedItem(item.itemType);

        // 기존 장비를 인벤토리로 반환
        if (previousEquip != null)
        {
            if (!_inventory.HasEmptySlot() && previousEquip != item)
            {
                Debug.LogWarning("[Equipment] 인벤토리 공간이 부족합니다.");
                return false;
            }

            if (previousEquip != item)
                _inventory.AddItem(previousEquip, 1);
        }

        // 인벤토리에서 제거
        _inventory.RemoveItemAt(inventorySlotIndex, 1);

        // 장착
        SetEquipSlot(item.itemType, item);
        RecalculateBonusStats();

        Debug.Log($"[Equipment] {item.itemName} 장착! (ATK+{item.bonusAttack}, DEF+{item.bonusDefense})");
        OnEquipmentChanged?.Invoke(item.itemType);
        return true;
    }

    /// <summary>장비를 해제합니다. 인벤토리로 돌아갑니다.</summary>
    public bool Unequip(Define.ItemType slotType)
    {
        ItemData equipped = GetEquippedItem(slotType);
        if (equipped == null) return false;

        if (!_inventory.HasEmptySlot())
        {
            Debug.LogWarning("[Equipment] 인벤토리 공간이 부족합니다.");
            return false;
        }

        _inventory.AddItem(equipped, 1);
        SetEquipSlot(slotType, null);
        RecalculateBonusStats();

        Debug.Log($"[Equipment] {equipped.itemName} 해제!");
        OnEquipmentChanged?.Invoke(slotType);
        return true;
    }

    // ════════════════════════════════════════════════════
    //  스탯 계산
    // ════════════════════════════════════════════════════

    private void RecalculateBonusStats()
    {
        BonusAttack = 0f;
        BonusDefense = 0f;
        BonusMaxHp = 0f;

        AddItemStats(_weapon);
        AddItemStats(_armor);
        AddItemStats(_accessory);
    }

    private void AddItemStats(ItemData item)
    {
        if (item == null) return;

        BonusAttack += item.bonusAttack;
        BonusDefense += item.bonusDefense;
        BonusMaxHp += item.bonusMaxHp;
    }

    // ════════════════════════════════════════════════════
    //  유틸리티
    // ════════════════════════════════════════════════════

    public ItemData GetEquippedItem(Define.ItemType slotType)
    {
        return slotType switch
        {
            Define.ItemType.Weapon => _weapon,
            Define.ItemType.Armor => _armor,
            Define.ItemType.Accessory => _accessory,
            _ => null
        };
    }

    private void SetEquipSlot(Define.ItemType slotType, ItemData item)
    {
        switch (slotType)
        {
            case Define.ItemType.Weapon: _weapon = item; break;
            case Define.ItemType.Armor: _armor = item; break;
            case Define.ItemType.Accessory: _accessory = item; break;
        }
    }
}