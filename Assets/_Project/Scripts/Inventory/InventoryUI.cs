using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

/// <summary>
/// 슬롯형 인벤토리 UI.
/// I키로 열고 닫으며, 아이템 클릭 시 사용/장착합니다.
/// 
/// [구조]
/// InventoryUI
///   ├── InventoryPanel (Grid Layout)
///   │   └── SlotPrefab x N
///   ├── EquipmentPanel
///   │   ├── WeaponSlot
///   │   ├── ArmorSlot
///   │   └── AccessorySlot
///   └── TooltipPanel
/// </summary>
public class InventoryUI : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  참조
    // ════════════════════════════════════════════════════

    [Header("Panels")]
    [SerializeField] private GameObject _inventoryPanel;
    [SerializeField] private Transform _slotContainer;
    [SerializeField] private GameObject _slotPrefab;

    [Header("Equipment Slots")]
    [SerializeField] private Image _weaponSlotIcon;
    [SerializeField] private Image _armorSlotIcon;
    [SerializeField] private Image _accessorySlotIcon;
    [SerializeField] private Button _weaponSlotButton;
    [SerializeField] private Button _armorSlotButton;
    [SerializeField] private Button _accessorySlotButton;

    [Header("Tooltip")]
    [SerializeField] private GameObject _tooltipPanel;
    [SerializeField] private TextMeshProUGUI _tooltipName;
    [SerializeField] private TextMeshProUGUI _tooltipDesc;
    [SerializeField] private TextMeshProUGUI _tooltipStats;
    [SerializeField] private Image _tooltipRarityBar;

    [Header("Stats Display")]
    [SerializeField] private TextMeshProUGUI _statsText;

    // ════════════════════════════════════════════════════
    //  내부
    // ════════════════════════════════════════════════════

    private InventorySystem _inventory;
    private EquipmentManager _equipment;
    private PlayerStats _playerStats;
    private PlayerInputHandler _playerInput;
    private MonoBehaviour _cinemachineInput;
    private InventorySlotUI[] _slotUIs;
    private bool _isOpen = false;

    public bool IsOpen => _isOpen;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(Define.Tag.Player);
        if (player != null)
        {
            _inventory = player.GetComponent<InventorySystem>();
            _equipment = player.GetComponent<EquipmentManager>();
            _playerStats = player.GetComponent<PlayerStats>();
            _playerInput = player.GetComponent<PlayerInputHandler>();

            if (_inventory != null)
                _inventory.OnSlotChanged += OnSlotChanged;

            if (_equipment != null)
                _equipment.OnEquipmentChanged += OnEquipmentChanged;
        }

        CreateSlots();
        SetupEquipmentButtons();
        CloseInventory();
        HideTooltip();

        // Cinemachine 카메라 입력 컴포넌트 캐싱
        foreach (var comp in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (comp.GetType().Name.Contains("CinemachineInputAxisController"))
            {
                _cinemachineInput = comp;
                break;
            }
        }
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.OnSlotChanged -= OnSlotChanged;

        if (_equipment != null)
            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
    }

    private void Update()
    {
        // I키로 인벤토리 토글
        if (UnityEngine.InputSystem.Keyboard.current != null &&
            UnityEngine.InputSystem.Keyboard.current.iKey.wasPressedThisFrame)
        {
            ToggleInventory();
        }
    }

    // ════════════════════════════════════════════════════
    //  열기 / 닫기
    // ════════════════════════════════════════════════════

    public void ToggleInventory()
    {
        if (_isOpen)
            CloseInventory();
        else
            OpenInventory();
    }

    public void OpenInventory()
    {
        _isOpen = true;
        _inventoryPanel.SetActive(true);

        // 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // 게임 입력 차단
        if (_playerInput != null)
        {
            _playerInput.InputSuppressed = true;
            _playerInput.ClearAllBuffers();
        }

        // 이동 즉시 정지
        GameObject player = GameObject.FindGameObjectWithTag(Define.Tag.Player);
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
            {
                controller.StopMovement();
                controller.SetCanMove(false);
            }
        }

        // 카메라 회전 차단
        if (_cinemachineInput != null)
            _cinemachineInput.enabled = false;

        RefreshAllSlots();
        RefreshEquipmentSlots();
        RefreshStatsDisplay();
    }

    public void CloseInventory()
    {
        _isOpen = false;
        _inventoryPanel.SetActive(false);
        HideTooltip();

        // 커서 숨김
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // 이동 복원
        GameObject player = GameObject.FindGameObjectWithTag(Define.Tag.Player);
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
                controller.SetCanMove(true);
        }

        // 게임 입력 복귀
        if (_playerInput != null)
        {
            _playerInput.ClearAllBuffers();
            _playerInput.InputSuppressed = false;
        }

        // 카메라 회전 복귀
        if (_cinemachineInput != null)
            _cinemachineInput.enabled = true;
    }

    // ════════════════════════════════════════════════════
    //  슬롯 생성 및 갱신
    // ════════════════════════════════════════════════════

    private void SetupEquipmentButtons()
    {
        if (_weaponSlotButton != null)
            _weaponSlotButton.onClick.AddListener(() => OnEquipSlotClicked(Define.ItemType.Weapon));
        if (_armorSlotButton != null)
            _armorSlotButton.onClick.AddListener(() => OnEquipSlotClicked(Define.ItemType.Armor));
        if (_accessorySlotButton != null)
            _accessorySlotButton.onClick.AddListener(() => OnEquipSlotClicked(Define.ItemType.Accessory));
    }

    private void CreateSlots()
    {
        if (_inventory == null || _slotPrefab == null || _slotContainer == null) return;

        _slotUIs = new InventorySlotUI[_inventory.SlotCount];

        for (int i = 0; i < _inventory.SlotCount; i++)
        {
            GameObject slotObj = Instantiate(_slotPrefab, _slotContainer);
            InventorySlotUI slotUI = slotObj.GetComponent<InventorySlotUI>();

            if (slotUI == null)
                slotUI = slotObj.AddComponent<InventorySlotUI>();

            int index = i;
            slotUI.Initialize(index, this);
            _slotUIs[i] = slotUI;
        }
    }

    private void OnSlotChanged(int slotIndex)
    {
        if (_slotUIs != null && slotIndex >= 0 && slotIndex < _slotUIs.Length)
            RefreshSlot(slotIndex);

        RefreshStatsDisplay();
    }

    private void OnEquipmentChanged(Define.ItemType slotType)
    {
        RefreshEquipmentSlots();
        RefreshStatsDisplay();
    }

    public void RefreshAllSlots()
    {
        if (_slotUIs == null || _inventory == null) return;

        for (int i = 0; i < _slotUIs.Length; i++)
            RefreshSlot(i);
    }

    private void RefreshSlot(int index)
    {
        if (_slotUIs == null || _inventory == null) return;

        ItemSlot slot = _inventory.GetSlot(index);
        _slotUIs[index].UpdateDisplay(slot);
    }

    private void RefreshEquipmentSlots()
    {
        if (_equipment == null) return;

        UpdateEquipIcon(_weaponSlotIcon, _equipment.Weapon);
        UpdateEquipIcon(_armorSlotIcon, _equipment.Armor);
        UpdateEquipIcon(_accessorySlotIcon, _equipment.Accessory);
    }

    private void UpdateEquipIcon(Image iconImage, ItemData item)
    {
        if (iconImage == null) return;

        if (item != null && item.icon != null)
        {
            iconImage.sprite = item.icon;
            iconImage.color = Color.white;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.color = new Color(1, 1, 1, 0.1f);
        }
    }

    private void RefreshStatsDisplay()
    {
        if (_statsText == null || _playerStats == null) return;

        _statsText.text =
                $"Lv. {_playerStats.Level}\n" +
                $"ATK: {_playerStats.TotalAttack:F0}\n" +
                $"DEF: {_playerStats.TotalDefense:F0}\n" +
                $"HP:  {_playerStats.MaxHp:F0}";
    }

    // ════════════════════════════════════════════════════
    //  슬롯 클릭 (InventorySlotUI에서 호출)
    // ════════════════════════════════════════════════════

    public void OnSlotClicked(int slotIndex)
    {
        if (_inventory == null) return;

        ItemSlot slot = _inventory.GetSlot(slotIndex);
        if (slot == null || slot.IsEmpty) return;

        ItemData item = slot.ItemData;

        if (item.IsEquipment)
        {
            _equipment?.Equip(item, slotIndex);
        }
        else if (item.IsConsumable)
        {
            _inventory.UseItem(slotIndex);
        }
    }

    /// <summary>장비 슬롯 클릭 (해제)</summary>
    public void OnEquipSlotClicked(Define.ItemType slotType)
    {
        _equipment?.Unequip(slotType);
    }

    // ════════════════════════════════════════════════════
    //  툴팁
    // ════════════════════════════════════════════════════

    public void ShowTooltip(ItemData item)
    {
        if (item == null || _tooltipPanel == null) return;

        _tooltipPanel.SetActive(true);

        if (_tooltipName != null)
        {
            _tooltipName.text = item.itemName;
            _tooltipName.color = item.GetRarityColor();
        }

        if (_tooltipDesc != null)
            _tooltipDesc.text = item.description;

        if (_tooltipStats != null)
        {
            string stats = "";
            if (item.bonusAttack > 0) stats += $"ATK +{item.bonusAttack:F0}\n";
            if (item.bonusDefense > 0) stats += $"DEF +{item.bonusDefense:F0}\n";
            if (item.bonusMaxHp > 0) stats += $"HP +{item.bonusMaxHp:F0}\n";
            if (item.IsConsumable && item.effectAmount > 0)
                stats += $"{item.consumableType}: +{item.effectAmount:F0}\n";

            _tooltipStats.text = stats.TrimEnd('\n');
        }

        if (_tooltipRarityBar != null)
            _tooltipRarityBar.color = item.GetRarityColor();
    }

    public void HideTooltip()
    {
        if (_tooltipPanel != null)
            _tooltipPanel.SetActive(false);
    }
}