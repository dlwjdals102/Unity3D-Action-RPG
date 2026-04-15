using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 인벤토리 슬롯 UI. 각 슬롯의 표시와 클릭을 처리합니다.
/// SlotPrefab에 부착합니다.
/// </summary>
public class InventorySlotUI : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _amountText;
    [SerializeField] private Image _rarityBorder;

    private int _slotIndex;
    private InventoryUI _inventoryUI;
    private ItemData _currentItem;

    public void Initialize(int index, InventoryUI inventoryUI)
    {
        _slotIndex = index;
        _inventoryUI = inventoryUI;

        // 버튼 클릭 연결
        var button = GetComponent<Button>();
        if (button == null)
            button = gameObject.AddComponent<Button>();

        button.onClick.AddListener(OnClick);
    }

    public void UpdateDisplay(ItemSlot slot)
    {
        if (slot == null || slot.IsEmpty)
        {
            _currentItem = null;

            if (_iconImage != null)
            {
                _iconImage.sprite = null;
                _iconImage.color = new Color(1, 1, 1, 0);
            }

            if (_amountText != null)
                _amountText.gameObject.SetActive(false);

            if (_rarityBorder != null)
                _rarityBorder.color = new Color(1, 1, 1, 0.1f);
        }
        else
        {
            _currentItem = slot.ItemData;

            if (_iconImage != null && slot.ItemData.icon != null)
            {
                _iconImage.sprite = slot.ItemData.icon;
                _iconImage.color = Color.white;
            }

            if (_amountText != null)
            {
                if (slot.Amount > 1)
                {
                    _amountText.gameObject.SetActive(true);
                    _amountText.text = slot.Amount.ToString();
                }
                else
                {
                    _amountText.gameObject.SetActive(false);
                }
            }

            // 희귀도 테두리 - 반투명으로 표시
            if (_rarityBorder != null)
            {
                Color rarityColor = slot.ItemData.GetRarityColor();
                rarityColor.a = 0.4f;
                _rarityBorder.color = rarityColor;
            }
        }
    }

    private void OnClick()
    {
        _inventoryUI?.OnSlotClicked(_slotIndex);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_currentItem != null)
            _inventoryUI?.ShowTooltip(_currentItem);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        _inventoryUI?.HideTooltip();
    }
}