using UnityEngine;

/// <summary>
/// 장착된 무기의 비주얼을 관리합니다.
/// 무기 장착 시 손 본에 모델을 생성하고, 해제 시 제거합니다.
/// Player 오브젝트에 부착합니다.
/// </summary>
public class WeaponVisual : MonoBehaviour
{
    [Header("References")]
    [Tooltip("무기가 부착될 손 본 (mixamorig:RightHand)")]
    [SerializeField] private Transform _handBone;

    [Header("Default")]
    [Tooltip("맨손일 때 표시할 모델 (없으면 비움)")]
    [SerializeField] private GameObject _unarmedPrefab;

    [Header("Weapon HitBox")]
    [Tooltip("무기 장착 시 크기가 변경될 HitBox")]
    [SerializeField] private HitBox _weaponHitBox;

    // ── 내부 ──
    private EquipmentManager _equipment;
    private PlayerAnimator _animator;
    private GameObject _currentWeaponModel;

    private void Start()
    {
        _equipment = GetComponent<EquipmentManager>();
        _animator = GetComponent<PlayerAnimator>();

        if (_equipment != null)
            _equipment.OnEquipmentChanged += OnEquipmentChanged;

        // 손 본 자동 탐색 (Inspector에서 설정 안 했을 때)
        if (_handBone == null)
            _handBone = FindHandBone();

        // 초기 상태 반영
        RefreshWeaponVisual();
    }

    private void OnDestroy()
    {
        if (_equipment != null)
            _equipment.OnEquipmentChanged -= OnEquipmentChanged;
    }

    private void OnEquipmentChanged(Define.ItemType slotType)
    {
        if (slotType == Define.ItemType.Weapon)
            RefreshWeaponVisual();
    }

    private void RefreshWeaponVisual()
    {
        // 기존 모델 제거
        if (_currentWeaponModel != null)
        {
            Destroy(_currentWeaponModel);
            _currentWeaponModel = null;
        }

        if (_handBone == null) return;

        // 장착된 무기 확인
        ItemData weapon = _equipment?.GetEquippedItem(Define.ItemType.Weapon);
        bool isArmed = weapon != null;

        // Animator에 무기 상태 전달
        _animator?.SetArmed(isArmed);

        if (weapon != null && weapon.dropPrefab != null)
        {
            // 무기 모델 생성
            _currentWeaponModel = Instantiate(weapon.dropPrefab, _handBone);
            _currentWeaponModel.transform.localPosition = Vector3.zero;
            _currentWeaponModel.transform.localRotation = Quaternion.identity;
            _currentWeaponModel.transform.localScale = Vector3.one;
            _currentWeaponModel.name = $"Visual_{weapon.itemName}";

            // 물리/픽업 컴포넌트 제거 (드롭 프리팹 재활용 시)
            RemovePickupComponents(_currentWeaponModel);

            // 히트박스 크기를 무기에 맞게 변경
            if (_weaponHitBox != null)
                _weaponHitBox.SetWeaponSize(weapon.weaponHitBoxSize, weapon.weaponHitBoxCenter);
        }
        else
        {
            if (_unarmedPrefab != null)
            {
                // 맨손 모델
                _currentWeaponModel = Instantiate(_unarmedPrefab, _handBone);
                _currentWeaponModel.transform.localPosition = Vector3.zero;
                _currentWeaponModel.transform.localRotation = Quaternion.identity;
                _currentWeaponModel.name = "Visual_Unarmed";
            }

            // 히트박스를 맨손 크기로 복원
            if (_weaponHitBox != null)
                _weaponHitBox.ApplyUnarmedSize();
        }

    }

    private void RemovePickupComponents(GameObject obj)
    {
        // ItemPickup 제거
        var pickup = obj.GetComponent<ItemPickup>();
        if (pickup != null) Destroy(pickup);

        // Rigidbody 제거
        var rb = obj.GetComponent<Rigidbody>();
        if (rb != null) Destroy(rb);

        // Collider를 Trigger가 아닌 상태로 유지하거나 제거
        var cols = obj.GetComponents<Collider>();
        foreach (var col in cols)
            Destroy(col);
    }

    private Transform FindHandBone()
    {
        // Mixamo 기준 오른손 본 탐색
        var allTransforms = GetComponentsInChildren<Transform>();
        foreach (var t in allTransforms)
        {
            if (t.name.Contains("RightHand") && !t.name.Contains("Index")
                && !t.name.Contains("Middle") && !t.name.Contains("Pinky")
                && !t.name.Contains("Ring") && !t.name.Contains("Thumb"))
            {
                return t;
            }
        }

        Debug.LogWarning("[WeaponVisual] RightHand 본을 찾을 수 없습니다. Inspector에서 직접 연결해주세요.");
        return null;
    }
}