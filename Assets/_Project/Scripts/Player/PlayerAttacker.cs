using UnityEngine;

/// <summary>
/// 플레이어의 공격 처리 컴포넌트.
/// 단일 책임: 공격 시 적 감지 + 데미지 전달 + 콤보 비용 (데미지/스태미나) 보유.
/// OverlapSphere 한 프레임 검사 방식 (단검의 순간 타격에 적합).
/// </summary>
public class PlayerAttacker : MonoBehaviour
{
    private PlayerStats _stats;
    private EquipmentManager _equipment;

    [Header("Hit Detection")]
    [SerializeField] private Transform _hitOrigin;
    [SerializeField] private float _hitRadius = 1f;
    [SerializeField] private LayerMask _enemyLayer;

    [Header("Combo Damage")]
    [Tooltip("콤보 단계별 데미지 배율. (기본공격 + 무기 데미지) × 배율. 1타, 2타")]
    [SerializeField] private float[] _comboMultipliers = { 1.0f, 1.3f }; /*1.8f };*/

    [Header("Combo Stamina Cost")]
    [SerializeField] private float[] _comboStaminaCosts = { 15f, 20f }; /*25f };*/

    private int _currentDamage;

    private void Awake()
    {
        if (_hitOrigin == null)
        {
            Debug.LogError("[PlayerAttacker] HitOrigin not assigned! Please assign in Inspector.");
        }

        _stats = GetComponent<PlayerStats>();
        _equipment = GetComponent<EquipmentManager>();
    }

    // ========================================================================
    // === Public API ===
    // ========================================================================

    /// <summary>
    /// 현재 콤보에 맞는 데미지 설정.
    /// AttackState 가 각 콤보 진행 시 호출.
    /// </summary>
    public void SetCurrentCombo(int comboIndex)
    {
        if (comboIndex < 0 || comboIndex >= _comboMultipliers.Length)
        {
            Debug.LogWarning($"[PlayerAttacker] Invalid combo index: {comboIndex}");
            return;
        }

        int weaponDamage = GetEquippedWeaponDamage();
        _currentDamage = CalculateDamage(weaponDamage, _comboMultipliers[comboIndex]);
    }

    /// <summary>
    /// 데미지 계산 공식: (기본공격 + 장비보너스 + 무기위력) × 배율.
    /// _stats.Attack 이 기본공격 + 장비 공격보너스를 포함한다.
    /// </summary>
    private int CalculateDamage(int weaponPower, float multiplier)
    {
        int baseAtk = _stats != null ? _stats.Attack : 0;
        return Mathf.RoundToInt((baseAtk + weaponPower) * multiplier);
    }

    /// <summary>현재 장착 무기의 기본 데미지. 무기 없으면 0.</summary>
    private int GetEquippedWeaponDamage()
    {
        if (_equipment == null) return 0;
        var weapon = _equipment.GetEquipped(EquipmentSlot.Weapon);
        return weapon != null ? weapon.WeaponDamage : 0;
    }

    /// <summary>맨손 발차기 데미지 설정. PerformHit 가 _currentDamage 를 사용.</summary>
    public void SetUnarmedAttack()
    {
        _currentDamage = CalculateDamage(0, 1.0f);
    }

    /// <summary>
    /// 콤보에 맞는 스태미나 비용 조회.
    /// AttackState 가 콤보 진행 가능 여부 체크 + 소모 시 호출.
    /// </summary>
    public float GetComboStaminaCost(int comboIndex)
    {
        if (comboIndex < 0 || comboIndex >= _comboStaminaCosts.Length)
        {
            Debug.LogWarning($"[PlayerAttacker] Invalid combo index: {comboIndex}");
            return 0f;
        }

        return _comboStaminaCosts[comboIndex];
    }

    /// <summary>
    /// 공격 타격 시점에 호출 (Animation Event 가 발사).
    /// OverlapSphere 한 프레임 검사로 영역 내 적 감지 + 데미지 적용.
    /// </summary>
    public void PerformHit()
    {
        if (_hitOrigin == null) return;

        Collider[] hits = Physics.OverlapSphere(_hitOrigin.position, _hitRadius, _enemyLayer);

        bool bigHit = false;
        bool hitAny = false;
        const int shakeThreshold = 25;   // 이 이상 데미지면 연출 (히트스톱+셰이크)

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                int amount = _currentDamage;

                var info = new DamageInfo
                {
                    Amount = amount,
                    Source = gameObject,
                    HitPoint = hit.ClosestPoint(_hitOrigin.position)
                };

                target.TakeDamage(info);
                hitAny = true;
                if (amount >= shakeThreshold) bigHit = true;
            }
        }

        if (hitAny)
        {
            var am = AudioManager.Instance;
            am?.PlaySound(am.Library.SwordImpact);
        }
        // 큰 적중만 연출 (히트스톱 + 셰이크 함께) - 약한/빠른 타엔 멈칫거림 없음
        if (bigHit)
        {
            HitStopManager.Instance?.Trigger();
            CameraShakeManager.Instance?.Shake();
        }
    }

    /// <summary>공격 휘두름 효과음. AttackState 가 공격 시작 시 호출 (헛스윙 포함).</summary>
    public void PlaySwing()
    {
        var am = AudioManager.Instance;
        am?.PlaySound(am.Library.SwordSwing);
    }

    // ========================================================================
    // === Editor Visualization ===
    // ========================================================================

    private void OnDrawGizmosSelected()
    {
        if (_hitOrigin == null) return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(_hitOrigin.position, _hitRadius);
    }
}