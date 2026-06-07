using UnityEngine;

/// <summary>
/// 인벤토리 격자 UI. Inventory 데이터를 읽어 칸들(InventorySlotUI)에 표시한다.
/// I 키로 열고 닫으며, 인벤토리 변경 시 자동 갱신 (이벤트 구독).
/// 
/// 칸은 고정 개수(미리 배치)로, 데이터가 있는 칸만 채우고 나머지는 빈 칸.
/// 제어 스크립트는 항상 활성인 곳에 두고 패널(_panelRoot)만 토글.
/// </summary>
public class InventoryUI : MonoBehaviour
{
    [Header("References")]
    [Tooltip("플레이어의 Inventory")]
    [SerializeField] private Inventory _inventory;

    [Tooltip("입력을 읽을 PlayerController")]
    [SerializeField] private PlayerController _controller;

    [Tooltip("열고 닫을 패널 루트 (토글 대상)")]
    [SerializeField] private GameObject _panelRoot;

    [Tooltip("격자 칸들 (고정 개수, 미리 배치)")]
    [SerializeField] private InventorySlotUI[] _slotUIs;

    [SerializeField] private EquipmentManager _equipment;

    private bool _isOpen;

    private void Awake()
    {
        if (_panelRoot != null) _panelRoot.SetActive(false);  // 시작은 닫힘
        _isOpen = false;
    }

    private void OnEnable()
    {
        if (_inventory != null) _inventory.OnInventoryChanged += Refresh;

        // 슬롯 클릭 콜백 등록
        if (_slotUIs != null)
        {
            foreach (var slotUI in _slotUIs)
            {
                if (slotUI != null) slotUI.OnSlotClicked += HandleSlotClicked;
            }
        }
    }

    private void OnDisable()
    {
        if (_inventory != null) _inventory.OnInventoryChanged -= Refresh;

        // 슬롯 클릭 콜백 해제
        if (_slotUIs != null)
        {
            foreach (var slotUI in _slotUIs)
            {
                if (slotUI != null) slotUI.OnSlotClicked -= HandleSlotClicked;
            }
        }
    }

    private void Update()
    {
        if (_controller == null) return;

        // I 키 토글
        if (_controller.ToggleInventoryRequested)
        {
            Toggle();
        }
    }

    private void Toggle()
    {
        _isOpen = !_isOpen;
        if (_panelRoot != null) _panelRoot.SetActive(_isOpen);

        // 인벤토리 열림: 커서 표시/해제, 닫힘: 다시 잠금/숨김
        if (_isOpen)
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            Refresh();
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    /// <summary>
    /// 인벤토리 데이터를 칸들에 반영.
    /// </summary>
    private void Refresh()
    {
        if (_slotUIs == null || _inventory == null) return;

        var slots = _inventory.Slots;

        for (int i = 0; i < _slotUIs.Length; i++)
        {
            if (_slotUIs[i] == null) continue;

            // 데이터 있는 칸은 채우고, 나머지는 빈 칸(null)
            InventorySlot slot = (i < slots.Count) ? slots[i] : null;
            _slotUIs[i].SetSlot(slot);
        }
    }

    /// <summary>인벤토리 슬롯 클릭 → 장비면 착용.</summary>
    private void HandleSlotClicked(ItemData item)
    {
        if (item is EquipmentData equipment && _equipment != null)
        {
            _equipment.Equip(equipment);
        }
        // (소비 아이템 사용 등은 나중에 분기 추가)
    }
}