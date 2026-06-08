using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

/// <summary>
/// 상점 판매 항목 하나. 아이콘/이름/가격을 표시하고, 클릭 시 구매를 요청한다.
/// 슬롯은 클릭 알림(이벤트)만 하고, 구매 처리는 ShopUI 가 담당 (느슨한 결합).
/// </summary>
public class ShopItemUI : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image _iconImage;
    [SerializeField] private TextMeshProUGUI _nameText;
    [SerializeField] private TextMeshProUGUI _priceText;

    private ItemData _item;

    /// <summary>항목 클릭 시 발행 (구매할 아이템 전달).</summary>
    public System.Action<ItemData> OnClicked;

    /// <summary>판매 아이템으로 항목 갱신.</summary>
    public void SetItem(ItemData item)
    {
        _item = item;
        if (item == null) return;

        if (_iconImage != null)
        {
            _iconImage.sprite = item.Icon;
            _iconImage.enabled = (item.Icon != null);
        }
        if (_nameText != null) _nameText.text = item.DisplayName;
        if (_priceText != null) _priceText.text = $"{item.Price}";
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_item != null) OnClicked?.Invoke(_item);
    }
}