using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 인벤토리 격자의 한 칸. 아이템 아이콘 + 개수를 표시한다.
/// 비어있으면 아이콘/개수를 숨긴다. InventoryUI 가 데이터로 갱신한다.
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerClickHandler
{
    [Tooltip("아이템 아이콘")]
    [SerializeField] private Image _iconImage;

    [Tooltip("개수 텍스트 (1개 또는 빈 칸이면 숨김)")]
    [SerializeField] private TextMeshProUGUI _countText;

    private InventorySlot _currentSlot;

    /// <summary>슬롯 클릭 시 발행 (담긴 아이템 전달). InventoryUI 가 구독해 착용 처리.</summary>
    public System.Action<ItemData> OnSlotClicked;       // 좌클릭 (착용)
    public System.Action<ItemData> OnSlotRightClicked;  // 우클릭 (판매 등)

    /// <summary>
    /// 슬롯 데이터로 칸을 갱신. null 이면 빈 칸.
    /// </summary>
    public void SetSlot(InventorySlot slot)
    {
        _currentSlot = slot;  // 클릭 시 참조용

        if (slot == null || slot.Item == null)
        {
            // 빈 칸
            if (_iconImage != null)
            {
                _iconImage.enabled = false;
            }
            if (_countText != null) _countText.text = "";
            return;
        }

        // 아이콘
        if (_iconImage != null)
        {
            _iconImage.sprite = slot.Item.Icon;
            _iconImage.enabled = (slot.Item.Icon != null);  // 아이콘 없으면 숨김
        }

        // 개수 (1개면 표시 안 함, 2개 이상만)
        if (_countText != null)
        {
            _countText.text = slot.Count > 1 ? slot.Count.ToString() : "";
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_currentSlot == null || _currentSlot.Item == null) return;

        if (eventData.button == PointerEventData.InputButton.Left)
            OnSlotClicked?.Invoke(_currentSlot.Item);
        else if (eventData.button == PointerEventData.InputButton.Right)
            OnSlotRightClicked?.Invoke(_currentSlot.Item);
    }
}