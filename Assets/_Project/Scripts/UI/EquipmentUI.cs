using UnityEngine;

/// <summary>
/// 장비 구역 UI. EquipmentManager 를 구독해 각 부위 슬롯에 착용 장비를 표시한다.
/// 슬롯 UI(EquipmentSlotUI)들은 미리 배치하고, 각자 담당 부위로 갱신한다.
/// </summary>
public class EquipmentUI : MonoBehaviour
{
    [SerializeField] private EquipmentManager _equipment;

    [Tooltip("장비 슬롯 UI들 (부위별, 미리 배치)")]
    [SerializeField] private EquipmentSlotUI[] _slotUIs;

    private void OnEnable()
    {
        if (_equipment != null) _equipment.OnEquipmentChanged += Refresh;

        // 슬롯 클릭 콜백 등록
        if (_slotUIs != null)
        {
            foreach (var slotUI in _slotUIs)
            {
                if (slotUI != null) slotUI.OnSlotClicked += HandleSlotClicked;
            }
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (_equipment != null) _equipment.OnEquipmentChanged -= Refresh;

        // 슬롯 클릭 콜백 해제
        if (_slotUIs != null)
        {
            foreach (var slotUI in _slotUIs)
            {
                if (slotUI != null) slotUI.OnSlotClicked -= HandleSlotClicked;
            }
        }
    }

    /// <summary>각 슬롯 UI 를 담당 부위의 착용 장비로 갱신.</summary>
    private void Refresh()
    {
        if (_slotUIs == null || _equipment == null) return;

        foreach (var slotUI in _slotUIs)
        {
            if (slotUI == null) continue;
            slotUI.SetEquipment(_equipment.GetEquipped(slotUI.Slot));
        }
    }

    /// <summary>장비 슬롯 클릭 → 해제.</summary>
    private void HandleSlotClicked(EquipmentSlot slot)
    {
        if (_equipment != null) _equipment.Unequip(slot);
    }
}