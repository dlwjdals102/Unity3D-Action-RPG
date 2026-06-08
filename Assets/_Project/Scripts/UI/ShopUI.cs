using UnityEngine;

/// <summary>
/// 상점 UI. ShopData 의 판매 목록을 표시하고, 클릭 시 구매를 처리한다.
/// 구매: 영혼이 충분하면 차감(PlayerSouls) + 인벤토리 추가(Inventory).
/// 제어 스크립트는 항상 활성, 패널(_panelRoot)만 토글.
/// </summary>
public class ShopUI : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerSouls _souls;
    [SerializeField] private Inventory _inventory;

    [Header("UI")]
    [SerializeField] private GameObject _panelRoot;
    [Tooltip("판매 항목 UI들 (미리 배치)")]
    [SerializeField] private ShopItemUI[] _itemUIs;

    private ShopData _shopData;  // 현재 열린 상점 데이터 (Shopkeeper 가 Open 시 전달)

    private void Awake()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        // 항목 클릭 콜백 등록
        if (_itemUIs != null)
        {
            foreach (var ui in _itemUIs)
            {
                if (ui != null) ui.OnClicked += HandlePurchase;
            }
        }
    }

    private void OnDisable()
    {
        if (_itemUIs != null)
        {
            foreach (var ui in _itemUIs)
            {
                if (ui != null) ui.OnClicked -= HandlePurchase;
            }
        }
    }

    /// <summary>상점 열기 (상호작용 시 호출).</summary>
    public void Open(ShopData shopData)
    {
        _shopData = shopData;  // 이 상점의 판매 목록으로 설정

        if (_panelRoot != null) _panelRoot.SetActive(true);
        Refresh();

        // 커서 표시 (인벤토리와 동일)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>상점 닫기.</summary>
    public void Close()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>판매 목록을 항목 UI에 매핑.</summary>
    private void Refresh()
    {
        if (_itemUIs == null || _shopData == null) return;

        var items = _shopData.ItemsForSale;
        for (int i = 0; i < _itemUIs.Length; i++)
        {
            if (_itemUIs[i] == null) continue;

            ItemData item = (i < items.Count) ? items[i] : null;
            _itemUIs[i].gameObject.SetActive(item != null);  // 빈 항목 숨김
            if (item != null) _itemUIs[i].SetItem(item);
        }
    }

    /// <summary>구매 처리: 영혼 충분하면 차감 + 인벤토리 추가.</summary>
    private void HandlePurchase(ItemData item)
    {
        if (item == null || _souls == null || _inventory == null) return;

        if (_souls.TrySpend(item.Price))
        {
            _inventory.AddItem(item, 1);
            Debug.Log($"[상점] 구매: {item.DisplayName} (-{item.Price} 영혼)");
        }
        else
        {
            Debug.Log($"[상점] 영혼 부족: {item.DisplayName} ({item.Price} 필요)");
        }
    }
}