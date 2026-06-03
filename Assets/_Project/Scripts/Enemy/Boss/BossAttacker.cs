using UnityEngine;

/// <summary>
/// 보스의 공격 타격 처리. IEnemyAttacker 구현.
/// 적 종류별 전용 Attacker 패턴 (Melee/Ranged/Elite 와 동일 구조).
/// 
/// - PerformHit: 기본 근접 공격 (Animation Event 가 호출). BossConfig.Damage 적용.
/// - PerformChargeHit: 돌진 충돌 (BossChargeState 가 호출). 강한 단발 + 맞춤 여부 반환.
/// 
/// (Phase 2 범위 공격 Slam 은 [2-2]에서 추가 예정)
/// 타격 메커니즘은 OverlapSphere + IDamageable.TakeDamage (공통 패턴).
/// </summary>
public class BossAttacker : MonoBehaviour, IEnemyAttacker
{
    [Header("Config")]
    [Tooltip("보스 수치 데이터 (기본/돌진 데미지 등)")]
    [SerializeField] private BossConfig _config;

    [Header("Hit Detection (기본 공격)")]
    [Tooltip("타격 감지 영역 중심 (보스 앞쪽 자식 GameObject)")]
    [SerializeField] private Transform _hitOrigin;

    [Tooltip("기본 공격 타격 반경 (m)")]
    [SerializeField] private float _hitRadius = 1.5f;

    [Tooltip("공격 대상 Layer (보통 Player)")]
    [SerializeField] private LayerMask _targetLayer;

    [Header("Charge Hit (돌진 충돌)")]
    [Tooltip("돌진 충돌 판정의 정면 오프셋 (보스 중심에서 진행 방향)")]
    [SerializeField] private float _chargeHitForwardOffset = 0.5f;

    [Tooltip("돌진 충돌 판정의 높이 오프셋 (발 기준, 가슴 높이로)")]
    [SerializeField] private float _chargeHitHeightOffset = 1f;

    [Tooltip("돌진 충돌 판정 반경 (m). 몸 충돌이라 넓게")]
    [SerializeField] private float _chargeHitRadius = 1.5f;

    private void Awake()
    {
        if (_config == null)
        {
            Debug.LogError($"[BossAttacker] BossConfig not assigned on {gameObject.name}!");
        }

        if (_hitOrigin == null)
        {
            Debug.LogError($"[BossAttacker] HitOrigin not assigned on {gameObject.name}!");
        }
    }

    // ========================================================================
    // === IEnemyAttacker 구현 ===
    // ========================================================================

    /// <summary>
    /// 기본 근접 공격 타격 (Animation Event 가 호출).
    /// OverlapSphere 로 영역 내 대상 감지 + BossConfig.Damage 적용.
    /// </summary>
    public void PerformHit()
    {
        if (_hitOrigin == null || _config == null) return;

        Collider[] hits = Physics.OverlapSphere(_hitOrigin.position, _hitRadius, _targetLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                var info = new DamageInfo
                {
                    Amount = _config.Damage,
                    Source = gameObject,
                    HitPoint = hit.ClosestPoint(_hitOrigin.position)
                };

                target.TakeDamage(info);
            }
        }
    }

    // ========================================================================
    // === 보스 전용 (돌진) ===
    // ========================================================================

    /// <summary>
    /// 돌진 충돌 타격. 보스 중심 기준 판정에 대상이 있으면 지정 데미지 적용.
    /// 맞췄는지 여부를 반환 (BossChargeState 가 충돌 종료 판단에 사용).
    /// </summary>
    /// <returns>대상(Player)을 맞췄으면 true</returns>
    public bool PerformChargeHit(int damage)
    {
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

    /// <summary>
    /// 내려찍기 타격 (BossSlamState 가 착지 시점에 호출).
    /// 보스 중심 원형 범위 (전방 오프셋 없음 - 제자리 강타, 돌진의 직선/전방과 다름).
    /// 착지 1회 판정이라 맞춤 여부 반환 없이 데미지만 적용.
    /// </summary>
    public void PerformSlam(int damage)
    {
        Vector3 slamCenter = GetSlamCenter();

        Collider[] hits = Physics.OverlapSphere(slamCenter, _config != null ? _config.SlamRadius : 3.5f, _targetLayer);

        foreach (var hit in hits)
        {
            if (hit.TryGetComponent<IDamageable>(out var target))
            {
                var info = new DamageInfo
                {
                    Amount = damage,
                    Source = gameObject,
                    HitPoint = hit.ClosestPoint(slamCenter)
                };

                target.TakeDamage(info);
            }
        }
    }


    // ========================================================================
    // === Helper + Editor Visualization ===
    // ========================================================================

    /// <summary>돌진 충돌 판정 중심점. 보스 중심 + 정면/높이 오프셋. PerformChargeHit 과 Gizmo 공유.</summary>
    private Vector3 GetChargeHitPoint()
    {
        return transform.position
             + transform.forward * _chargeHitForwardOffset
             + Vector3.up * _chargeHitHeightOffset;
    }

    /// <summary>내려찍기 판정 중심점. 보스 발밑 중심 (전방 오프셋 없음 - 제자리 원형).</summary>
    private Vector3 GetSlamCenter()
    {
        return transform.position;
    }

    private void OnDrawGizmosSelected()
    {
        // 기본 공격 범위 (빨강)
        if (_hitOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_hitOrigin.position, _hitRadius);
        }

        // 돌진 충돌 범위 (주황)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(GetChargeHitPoint(), _chargeHitRadius);

        // 내려찍기 범위 (노랑) - 보스 중심 원형
        Gizmos.color = Color.yellow;
        float slamR = _config != null ? _config.SlamRadius : 3.5f;
        Gizmos.DrawWireSphere(GetSlamCenter(), slamR);
    }
}