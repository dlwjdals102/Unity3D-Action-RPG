using UnityEngine;

/// <summary>
/// 지정한 장비 슬롯의 착용 여부에 따라 모델을 표시/숨김.
/// EquipmentManager.OnEquipmentChanged 구독 → 해당 슬롯 유무로 toggle.
/// 검(Weapon)·방패(Shield) 등 슬롯별로 컴포넌트 하나씩 사용.
/// </summary>
public class EquipmentVisual : MonoBehaviour
{
    [Tooltip("이 비주얼이 대응하는 장비 슬롯")]
    [SerializeField] private EquipmentSlot _slot = EquipmentSlot.Weapon;

    [Tooltip("소켓에 붙인 장비 모델 (기본 숨김)")]
    [SerializeField] private GameObject _model;
    [SerializeField] private EquipmentManager _equipment;

    private void Awake()
    {
        if (_equipment == null) _equipment = GetComponentInParent<EquipmentManager>();
    }

    private void OnEnable()
    {
        if (_equipment != null) _equipment.OnEquipmentChanged += Refresh;
        Refresh();
    }

    private void OnDisable()
    {
        if (_equipment != null) _equipment.OnEquipmentChanged -= Refresh;
    }

    private void Refresh()
    {
        if (_model == null || _equipment == null) return;
        _model.SetActive(_equipment.GetEquipped(_slot) != null);
    }
}