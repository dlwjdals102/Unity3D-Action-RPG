using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 장비 구역의 한 슬롯. 특정 부위(EquipmentSlot)를 담당하며,
/// 그 부위에 착용된 장비의 아이콘을 표시한다 (없으면 빈 칸).
/// EquipmentManager 가 갱신 시 SetEquipment 로 채운다.
/// </summary>
public class EquipmentSlotUI : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("이 슬롯이 담당하는 부위")]
    [SerializeField] private EquipmentSlot _slot;

    [Tooltip("착용 장비 아이콘")]
    [SerializeField] private Image _iconImage;

    /// <summary>장비 슬롯 클릭 시 발행 (부위 전달). EquipmentUI 가 구독해 해제.</summary>
    public System.Action<EquipmentSlot> OnSlotClicked;

    /// <summary>이 슬롯의 담당 부위.</summary>
    public EquipmentSlot Slot => _slot;

    /// <summary>착용 장비로 갱신. null 이면 빈 슬롯.</summary>
    public void SetEquipment(EquipmentData equipment)
    {
        if (equipment == null || equipment.Icon == null)
        {
            if (_iconImage != null) _iconImage.enabled = false;
            return;
        }

        if (_iconImage != null)
        {
            _iconImage.sprite = equipment.Icon;
            _iconImage.enabled = true;
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnSlotClicked?.Invoke(_slot);
    }
}