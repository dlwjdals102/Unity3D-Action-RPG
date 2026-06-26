using UnityEngine;

/// <summary>
/// 보스의 공격 타격 처리. IEnemyAttacker 구현.
/// - PerformHit: 근접 콤보 타격 (Animation Event 가 호출). BossConfig.Damage 적용.
/// (활 발사 FireArrow 는 Bow 스포크에서 추가)
/// </summary>
public class BossAttacker : MonoBehaviour, IEnemyAttacker
{
    [Header("Config")]
    [SerializeField] private BossConfig _config;

    [Header("Hit Detection (근접 공격)")]
    [Tooltip("타격 감지 영역 중심 (보스 앞쪽 자식 GameObject)")]
    [SerializeField] private Transform _hitOrigin;

    [Tooltip("근접 공격 타격 반경 (m)")]
    [SerializeField] private float _hitRadius = 1.5f;

    [Tooltip("공격 대상 Layer (보통 Player)")]
    [SerializeField] private LayerMask _targetLayer;

    // 현재 근접 패턴의 데미지 (BossMeleeAttackStateBase 가 OnEnter 에 설정).
    // 패턴(발차기/베기)마다 다르므로 PerformHit 직전에 주입받는다.
    private int _currentAttackDamage;

    [Header("Bow (활 발사)")]
    [Tooltip("화살 생성 위치 (보스의 활/손 자식 GameObject)")]
    [SerializeField] private Transform _fireOrigin;

    // 발사 방향 고정 (드로우 시작 시점 캡처) - 원거리 적 조준락과 동일 철학
    private Vector3 _lockedArrowDir;
    private bool _hasLockedArrow;

    private void Awake()
    {
        if (_config == null)
            Debug.LogError($"[BossAttacker] BossConfig not assigned on {gameObject.name}!");
        if (_hitOrigin == null)
            Debug.LogError($"[BossAttacker] HitOrigin not assigned on {gameObject.name}!");

        _currentAttackDamage = _config != null ? _config.Damage : 0;
    }

    /// <summary>
    /// 다음 PerformHit 이 적용할 데미지 설정. 근접 패턴 상태가 OnEnter 에 호출.
    /// (Elite 의 SetCurrentCombo 와 동일 발상 - 패턴별 데미지)
    /// </summary>
    public void SetAttackDamage(int damage)
    {
        _currentAttackDamage = damage;
    }

    /// <summary>
    /// 근접 공격 타격 (Animation Event 가 호출). OverlapSphere + BossConfig.Damage.
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
                    Amount = _currentAttackDamage,
                    Source = gameObject,
                    HitPoint = hit.ClosestPoint(_hitOrigin.position)
                };
                target.TakeDamage(info);
            }
        }
    }

    // ========================================================================
    // === 활 (Bow) - BossBowState 가 구동 ===
    // ========================================================================

    /// <summary>
    /// 드로우 시작 시점에 발사 방향 1회 고정. BossBowState 가 호출.
    /// 이후 FireArrow 가 이 방향으로 발사 → 윈드업 중 플레이어 회피 여지.
    /// </summary>
    public void LockArrowAim(Transform target)
    {
        if (target == null || _fireOrigin == null) { _hasLockedArrow = false; return; }

        Vector3 dir = target.position - _fireOrigin.position;
        dir.y = 0;
        if (dir.sqrMagnitude < 0.01f) { _hasLockedArrow = false; return; }

        _lockedArrowDir = dir.normalized;
        _hasLockedArrow = true;
    }

    /// <summary>
    /// 고정된 방향으로 화살 1발 발사. BossBowState 가 윈드업 종료 시 호출.
    /// 락이 없으면(미설정/너무 가까움) 발사 생략.
    /// </summary>
    public void FireArrow()
    {
        if (!_hasLockedArrow || _fireOrigin == null || _config == null) return;
        if (_config.BowProjectilePrefab == null)
        {
            Debug.LogError($"[BossAttacker] BowProjectilePrefab not set in BossConfig on {gameObject.name}!");
            return;
        }

        GameObject projObj = Instantiate(
            _config.BowProjectilePrefab,
            _fireOrigin.position,
            Quaternion.LookRotation(_lockedArrowDir)
        );

        if (projObj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.Initialize(
                _config.BowProjectileSpeed,
                _config.BowDamage,
                _targetLayer,      // 근접과 동일 (플레이어)
                gameObject         // owner
            );
        }
        else
        {
            Debug.LogError($"[BossAttacker] BowProjectilePrefab has no Projectile component on {gameObject.name}!");
            Destroy(projObj);
        }

        _hasLockedArrow = false;  // 1발 소비
    }

    private void OnDrawGizmosSelected()
    {
        if (_hitOrigin != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(_hitOrigin.position, _hitRadius);
        }

        if (_fireOrigin != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(_fireOrigin.position, 0.15f);
        }
    }
}