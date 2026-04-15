using System;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// 인벤토리 슬롯. 아이템 데이터와 수량을 보유합니다.
/// </summary>
[System.Serializable]
public class ItemSlot
{
    public ItemData ItemData;
    public int Amount;

    public bool IsEmpty => ItemData == null || Amount <= 0;

    public ItemSlot()
    {
        ItemData = null;
        Amount = 0;
    }

    public ItemSlot(ItemData data, int amount)
    {
        ItemData = data;
        Amount = amount;
    }

    public void Clear()
    {
        ItemData = null;
        Amount = 0;
    }

    /// <summary>가능한 만큼 추가하고 남은 수량 반환</summary>
    public int AddAmount(int amount)
    {
        if (ItemData == null) return amount;

        int canAdd = ItemData.maxStack - Amount;
        int toAdd = Mathf.Min(amount, canAdd);
        Amount += toAdd;
        return amount - toAdd;
    }

    /// <summary>수량 감소. 0이 되면 슬롯 비움</summary>
    public void RemoveAmount(int amount)
    {
        Amount -= amount;
        if (Amount <= 0)
            Clear();
    }
}

/// <summary>
/// 인벤토리 시스템. 고정 크기 슬롯 배열로 아이템을 관리합니다.
/// 
/// [면접 포인트]
/// - 고정 슬롯 배열: UI와 1:1 매핑으로 관리가 간단
/// - 중첩 로직: 같은 아이템을 기존 슬롯에 먼저 채우고 빈 슬롯 사용
/// - 이벤트 기반: UI가 변경을 자동으로 감지
/// </summary>
public class InventorySystem : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  설정
    // ════════════════════════════════════════════════════

    [Header("Settings")]
    [SerializeField] private int _slotCount = 20;

    // ════════════════════════════════════════════════════
    //  데이터
    // ════════════════════════════════════════════════════

    private ItemSlot[] _slots;

    public ItemSlot[] Slots => _slots;
    public int SlotCount => _slotCount;

    // ── 이벤트 ──
    /// <summary>인벤토리 내용이 변경될 때 (슬롯 인덱스)</summary>
    public event Action<int> OnSlotChanged;

    /// <summary>아이템 추가 시 (아이템 데이터, 수량)</summary>
    public event Action<ItemData, int> OnItemAdded;

    /// <summary>아이템 제거 시</summary>
    public event Action<ItemData, int> OnItemRemoved;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        _slots = new ItemSlot[_slotCount];
        for (int i = 0; i < _slotCount; i++)
            _slots[i] = new ItemSlot();
    }

    // ════════════════════════════════════════════════════
    //  아이템 추가
    // ════════════════════════════════════════════════════

    /// <summary>
    /// 아이템을 인벤토리에 추가합니다.
    /// 중첩 가능하면 기존 슬롯에 먼저 추가하고, 빈 슬롯에 배치합니다.
    /// </summary>
    /// <returns>추가 성공 여부</returns>
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        int remaining = amount;

        // 1단계: 중첩 가능한 기존 슬롯에 채우기
        if (item.IsStackable)
        {
            for (int i = 0; i < _slotCount && remaining > 0; i++)
            {
                if (_slots[i].ItemData == item && _slots[i].Amount < item.maxStack)
                {
                    remaining = _slots[i].AddAmount(remaining);
                    OnSlotChanged?.Invoke(i);
                }
            }
        }

        // 2단계: 빈 슬롯에 배치
        while (remaining > 0)
        {
            int emptySlot = FindEmptySlot();
            if (emptySlot == -1)
            {
                Debug.LogWarning("[Inventory] 인벤토리가 가득 찼습니다.");
                return false;
            }

            int toPlace = Mathf.Min(remaining, item.maxStack);
            _slots[emptySlot] = new ItemSlot(item, toPlace);
            remaining -= toPlace;
            OnSlotChanged?.Invoke(emptySlot);
        }

        OnItemAdded?.Invoke(item, amount);
        Debug.Log($"[Inventory] {item.itemName} x{amount} 획득!");
        return true;
    }

    // ════════════════════════════════════════════════════
    //  아이템 제거
    // ════════════════════════════════════════════════════

    /// <summary>특정 슬롯에서 아이템을 제거합니다.</summary>
    public bool RemoveItemAt(int slotIndex, int amount = 1)
    {
        if (!IsValidSlot(slotIndex)) return false;
        if (_slots[slotIndex].IsEmpty) return false;

        ItemData item = _slots[slotIndex].ItemData;
        _slots[slotIndex].RemoveAmount(amount);
        OnSlotChanged?.Invoke(slotIndex);
        OnItemRemoved?.Invoke(item, amount);
        return true;
    }

    /// <summary>아이템 데이터로 제거합니다.</summary>
    public bool RemoveItem(ItemData item, int amount = 1)
    {
        int remaining = amount;

        for (int i = 0; i < _slotCount && remaining > 0; i++)
        {
            if (_slots[i].ItemData == item)
            {
                int toRemove = Mathf.Min(remaining, _slots[i].Amount);
                _slots[i].RemoveAmount(toRemove);
                remaining -= toRemove;
                OnSlotChanged?.Invoke(i);
            }
        }

        if (remaining < amount)
            OnItemRemoved?.Invoke(item, amount - remaining);

        return remaining == 0;
    }

    // ════════════════════════════════════════════════════
    //  아이템 사용
    // ════════════════════════════════════════════════════

    /// <summary>슬롯의 아이템을 사용합니다.</summary>
    public bool UseItem(int slotIndex)
    {
        if (!IsValidSlot(slotIndex)) return false;
        if (_slots[slotIndex].IsEmpty) return false;

        ItemData item = _slots[slotIndex].ItemData;

        if (item.IsConsumable)
        {
            ApplyConsumableEffect(item);
            RemoveItemAt(slotIndex, 1);
            return true;
        }

        if (item.IsEquipment)
        {
            // 장비는 EquipmentManager에서 처리
            return false;
        }

        return false;
    }

    private void ApplyConsumableEffect(ItemData item)
    {
        var health = GetComponent<PlayerHealth>();

        switch (item.consumableType)
        {
            case ConsumableType.HealHp:
                if (health != null) health.Heal(item.effectAmount);
                Debug.Log($"[Inventory] {item.itemName} 사용! HP +{item.effectAmount}");
                break;
        }
    }

    // ════════════════════════════════════════════════════
    //  조회
    // ════════════════════════════════════════════════════

    /// <summary>특정 아이템의 총 보유 수량</summary>
    public int GetItemCount(ItemData item)
    {
        int count = 0;
        for (int i = 0; i < _slotCount; i++)
        {
            if (_slots[i].ItemData == item)
                count += _slots[i].Amount;
        }
        return count;
    }

    /// <summary>특정 아이템을 보유하고 있는지</summary>
    public bool HasItem(ItemData item, int amount = 1)
    {
        return GetItemCount(item) >= amount;
    }

    /// <summary>인벤토리에 빈 공간이 있는지</summary>
    public bool HasEmptySlot()
    {
        return FindEmptySlot() != -1;
    }

    public ItemSlot GetSlot(int index)
    {
        return IsValidSlot(index) ? _slots[index] : null;
    }

    // ════════════════════════════════════════════════════
    //  유틸리티
    // ════════════════════════════════════════════════════

    private int FindEmptySlot()
    {
        for (int i = 0; i < _slotCount; i++)
        {
            if (_slots[i].IsEmpty) return i;
        }
        return -1;
    }

    private bool IsValidSlot(int index)
    {
        return index >= 0 && index < _slotCount;
    }
}