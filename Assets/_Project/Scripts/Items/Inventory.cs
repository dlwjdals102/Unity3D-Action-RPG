using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 인벤토리 한 칸. 아이템 종류 + 개수.
/// </summary>
[Serializable]
public class InventorySlot
{
    public ItemData Item;
    public int Count;

    public InventorySlot(ItemData item, int count)
    {
        Item = item;
        Count = count;
    }
}

/// <summary>
/// 플레이어 인벤토리. 보유 아이템(슬롯 목록)을 관리한다.
/// 추가 시 중첩 가능 아이템(MaxStack>1)은 기존 슬롯에 쌓고, 아니면 새 슬롯.
/// 변경 시 OnInventoryChanged 발행 → UI 갱신 (이벤트 기반).
/// </summary>
public class Inventory : MonoBehaviour
{
    [Tooltip("최대 슬롯 수 (격자 칸 수)")]
    [SerializeField] private int _maxSlots = 20;

    private readonly List<InventorySlot> _slots = new List<InventorySlot>();

    /// <summary>
    /// 인벤토리 변경 시 발행 (UI 갱신용).
    /// </summary>
    public event Action OnInventoryChanged;

    /// <summary>
    /// 현재 슬롯 목록 (읽기용).
    /// </summary>
    public IReadOnlyList<InventorySlot> Slots => _slots;

    /// <summary>
    /// 아이템 추가. 중첩 가능하면 기존 슬롯에 쌓고, 아니면 새 슬롯.
    /// 슬롯이 꽉 차면 추가 실패 (false).
    /// </summary>
    public bool AddItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return false;

        // 1. 중첩 가능 아이템이면 기존 슬롯에 채우기
        if (item.MaxStack > 1)
        {
            foreach (var slot in _slots)
            {
                if (slot.Item == item && slot.Count < item.MaxStack)
                {
                    int space = item.MaxStack - slot.Count;
                    int toAdd = Mathf.Min(space, amount);
                    slot.Count += toAdd;
                    amount -= toAdd;
                    if (amount <= 0)
                    {
                        OnInventoryChanged?.Invoke();
                        return true;
                    }
                }
            }
        }

        // 2. 남은 수량은 새 슬롯에 (공간 있으면)
        while (amount > 0)
        {
            if (_slots.Count >= _maxSlots)
            {
                OnInventoryChanged?.Invoke();  // 일부라도 들어갔으면 갱신
                return false;  // 슬롯 부족
            }

            int stack = Mathf.Min(amount, item.MaxStack);
            _slots.Add(new InventorySlot(item, stack));
            amount -= stack;
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 아이템 제거 (지정 수량). 보유보다 많이 요청하면 가능한 만큼만.
    /// </summary>
    public void RemoveItem(ItemData item, int amount = 1)
    {
        if (item == null || amount <= 0) return;

        for (int i = _slots.Count - 1; i >= 0 && amount > 0; i--)
        {
            if (_slots[i].Item != item) continue;

            int toRemove = Mathf.Min(_slots[i].Count, amount);
            _slots[i].Count -= toRemove;
            amount -= toRemove;

            if (_slots[i].Count <= 0)
            {
                _slots.RemoveAt(i);  // 빈 슬롯 제거
            }
        }

        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 특정 아이템 보유 총 개수.
    /// </summary>
    public int GetCount(ItemData item)
    {
        int total = 0;
        foreach (var slot in _slots)
        {
            if (slot.Item == item) total += slot.Count;
        }
        return total;
    }
}