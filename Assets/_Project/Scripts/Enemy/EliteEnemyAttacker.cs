using UnityEngine;

/// <summary>
/// 엘리트 적의 근접 콤보 공격 처리. IEnemyAttacker 구현.
/// 근접 EnemyAttacker (단일 데미지) 와 달리 콤보 인덱스별 데미지를 적용한다.
/// 
/// IEnemyAttacker.PerformHit() 은 매개변수가 없으므로 (Animation Event 가 호출),
/// State 가 각 타 시작 시 SetCurrentCombo(index) 로 현재 데미지를 미리 설정한다.
/// (PlayerAttacker 의 SetCurrentCombo 패턴 일관)
/// 
/// 데미지는 EliteEnemyConfig.GetComboDamage(index) 에서 읽는다.
/// </summary>
public class EliteEnemyAttacker : MonoBehaviour, IEnemyAttacker
{
    [Header("Config")]
    [Tooltip("엘리트 적의 수치 데이터 (콤보 데미지 등)")]
    [SerializeField] private EliteEnemyConfig _config;

    [Header("Hit Detection")]
    [Tooltip("타격 감지 영역의 중심 (적의 앞쪽 자식 GameObject)")]
    [SerializeField] private Transform _hitOrigin;

    [Tooltip("타격 감지 반경 (m)")]
    [SerializeField] private float _hitRadius = 1f;

    [Tooltip("공격 대상 Layer (보통 Player)")]
    [SerializeField] private LayerMask _targetLayer;

    [Header("Charge Hit (돌진 충돌)")]
    [Tooltip("돌진 충돌 판정의 정면 오프셋 (적 중심에서 진행 방향으로). 몸 충돌이라 HitOrigin 보다 가까움")]
    [SerializeField] private float _chargeHitForwardOffset = 0.5f;

    [Tooltip("돌진 충돌 판정의 높이 오프셋 (적 발 기준). Base Offset 고려해 가슴 높이로")]
    [SerializeField] private float _chargeHitHeightOffset = 1f;

    [Tooltip("돌진 충돌 판정 반경 (m). 몸 충돌이라 콤보보다 넓을 수 있음")]
    [SerializeField] private float _chargeHitRadius = 1f;

    // 현재 타의 데미지 (SetCurrentCombo 로 설정)
    private int _currentDamage;

    private void Awake()
    {
        if (_config == null)
        {
            Debug.LogError($"[EliteEnemyAttacker] EliteEnemyConfig not assigned on {gameObject.name}!");
        }

        if (_hitOrigin == null)
        {
            Debug.LogError($"[EliteEnemyAttacker] HitOrigin not assigned on {gameObject.name}!");
        }
    }

    // ========================================================================
    // === Public API (State 가 호출) ===
    // ========================================================================

    /// <summary>
    /// 현재 콤보 타의 데미지를 설정한다. State 가 각 타 시작 시 호출.
    /// PerformHit (Animation Event) 가 이 데미지를 적용.
    /// </summary>
    public void SetCurrentCombo(int comboIndex)
    {
        if (_config == null) return;
        _currentDamage = _config.GetComboDamage(comboIndex);
    }

    // ========================================================================
    // === IEnemyAttacker 구현 ===
    // ========================================================================

    /// <summary>
    /// 공격 타격 시점에 호출 (Animation Event 가 발사).
    /// OverlapSphere 로 영역 내 대상 감지 + 현재 콤보 데미지 적용.
    /// </summary>
    public void PerformHit()
    {
        if (_hitOrigin == null) return;

        Collider[] hits = Physics.OverlapSphere(_hitOrigin.position, _hitRadius, _targetLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                var info = new DamageInfo
                {
                    Amount = _currentDamage,
                    Source = gameObject,
                    HitPoint = hit.ClosestPoint(_hitOrigin.position)
                };

                target.TakeDamage(info);
            }
        }
    }

    /// <summary>
    /// 돌진 충돌 타격. HitOrigin 영역에 대상이 있으면 지정 데미지를 적용한다.
    /// 콤보(PerformHit)와 달리 데미지를 매개변수로 받고(돌진 데미지),
    /// 대상을 맞췄는지 여부를 반환한다 (ChargeState 가 충돌 종료 판단에 사용).
    /// </summary>
    /// <returns>대상(Player)을 맞췄으면 true</returns>
    public bool PerformChargeHit(int damage)
    {
        // 돌진은 "몸으로 부딪히는" 공격이라 콤보용 HitOrigin(앞쪽 리치)이 아닌
        // 적 중심 기준으로 판정한다 (오프셋/반경은 Inspector 조정 가능).
        Vector3 chargeHitPoint = GetChargeHitPoint();

        Collider[] hits = Physics.OverlapSphere(chargeHitPoint, _chargeHitRadius, _targetLayer);

        bool hitTarget = false;

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                var info = new DamageInfo
                {
                    Amount = damage,
                    Source = gameObject,
                    HitPoint = hit.ClosestPoint(chargeHitPoint)
                };

                target.TakeDamage(info);
                hitTarget = true;
            }
        }

        return hitTarget;
    }

    // ========================================================================
    // === Editor Visualization ===
    // ========================================================================

    /// <summary>
    /// 돌진 충돌 판정 중심점. 적 중심 + 정면 오프셋 + 높이 오프셋.
    /// PerformChargeHit 과 Gizmo 가 공유 (단일 출처).
    /// </summary>
    private Vector3 GetChargeHitPoint()
    {
        return transform.position
             + transform.forward * _chargeHitForwardOffset
             + Vector3.up * _chargeHitHeightOffset;
    }

    private void OnDrawGizmosSelected()
    {
        // 콤보 타격 범위 (빨강) - HitOrigin 앞쪽 리치
        if (_hitOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_hitOrigin.position, _hitRadius);
        }

        // 돌진 충돌 범위 (주황) - 적 중심 + 오프셋
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(GetChargeHitPoint(), _chargeHitRadius);
    }
}