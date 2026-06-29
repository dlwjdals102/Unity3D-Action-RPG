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
    [Header("판매 목록 (스크롤)")]
    [Tooltip("아이템 행 프리팹 (ShopItemUI)")]
    [SerializeField] private ShopItemUI _itemPrefab;
    [Tooltip("ScrollView 의 Content (행이 생성될 부모)")]
    [SerializeField] private Transform _content;

    [Tooltip("상점 내 '내 인벤토리(판매)' 슬롯 UI들 - 미리 배치")]
    [SerializeField] private InventorySlotUI[] _sellUIs;

    [Tooltip("판매가 비율 (매입가 대비). 0.5 = 절반")]
    [SerializeField] private float _sellRatio = 0.5f;

    private ShopData _shopData;  // 현재 열린 상점 데이터 (Shopkeeper 가 Open 시 전달)

    private void Awake()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (_sellUIs != null)
        {
            foreach (var ui in _sellUIs)
                if (ui != null) ui.OnSlotRightClicked += HandleSell;
        }

        if (_inventory != null) _inventory.OnInventoryChanged += RefreshSell;
    }

    private void OnDisable()
    {
        if (_sellUIs != null)
        {
            foreach (var ui in _sellUIs)
                if (ui != null) ui.OnSlotRightClicked -= HandleSell;
        }

        if (_inventory != null) _inventory.OnInventoryChanged -= RefreshSell;
    }

    /// <summary>상점 열기 (상호작용 시 호출).</summary>
    public void Open(ShopData shopData)
    {
        _shopData = shopData;  // 이 상점의 판매 목록으로 설정

        if (_panelRoot != null) _panelRoot.SetActive(true);
        Refresh();
        RefreshSell();   // 열 때 내 인벤토리 채움

        // 커서 표시 (인벤토리와 동일)
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        UIInputLock.Push();
    }

    /// <summary>상점 닫기.</summary>
    public void Close()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        UIInputLock.Pop();
    }

    /// <summary>판매 목록을 항목 UI에 매핑.</summary>
    private void Refresh()
    {
        if (_content == null || _itemPrefab == null || _shopData == null) return;

        // 기존 행 제거
        for (int i = _content.childCount - 1; i >= 0; i--)
            Destroy(_content.GetChild(i).gameObject);

        // 아이템마다 행 생성 + 구매 구독
        foreach (var item in _shopData.ItemsForSale)
        {
            if (item == null) continue;
            ShopItemUI row = Instantiate(_itemPrefab, _content);
            row.SetItem(item);
            row.OnClicked += HandlePurchase;   // 행이 파괴되면 구독도 같이 사라짐 (누수 없음)
        }
    }

    /// <summary>구매 처리: 영혼 충분하면 차감 + 인벤토리 추가.</summary>
    private void HandlePurchase(ItemData item)
    {
        if (item == null || _souls == null || _inventory == null) return;

        if (_souls.TrySpend(item.Price)) 
            _inventory.AddItem(item, 1);
    }

    /// <summary>내 인벤토리를 판매 슬롯 UI에 매핑 (InventoryUI.Refresh 와 동일 패턴).</summary>
    private void RefreshSell()
    {
        if (_sellUIs == null || _inventory == null) return;

        var slots = _inventory.Slots;
        for (int i = 0; i < _sellUIs.Length; i++)
        {
            if (_sellUIs[i] == null) continue;
            InventorySlot slot = (i < slots.Count) ? slots[i] : null;
            _sellUIs[i].SetSlot(slot);
        }
    }

    /// <summary>판매: 매입가의 _sellRatio 만큼 영혼 지급 + 인벤에서 1개 제거.</summary>
    private void HandleSell(ItemData item)
    {
        if (item == null || _souls == null || _inventory == null) return;

        int sellPrice = Mathf.RoundToInt(item.Price * _sellRatio);
        _inventory.RemoveItem(item, 1);
        _souls.Add(sellPrice);
        // 목록 갱신은 OnInventoryChanged → RefreshSell 로 자동
    }
}