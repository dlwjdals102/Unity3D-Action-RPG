using UnityEngine;

/// <summary>
/// 원거리 적의 공격 처리. IEnemyAttacker 구현.
/// 근접 MeleeEnemyAttacker (OverlapSphere 즉시 타격) 와 달리, FireOrigin 에서 발사체를 생성한다.
/// 
/// 발사체 시각/수치 (Prefab, 속도, 데미지) 는 RangedEnemyConfig 에서 읽는다.
/// → 같은 코드 + 다른 Config = 궁수(화살) / 마법사(마법 구체).
/// 
/// PerformHit (Animation Event 호출) 시점에 Target 방향으로 발사체 1개 생성.
/// </summary>
public class RangedEnemyAttacker : MonoBehaviour, IEnemyAttacker
{
    [Header("Config")]
    [Tooltip("원거리 적의 수치 데이터 (발사체 Prefab, 속도, 데미지 등)")]
    [SerializeField] private RangedEnemyConfig _config;

    [Header("Fire")]
    [Tooltip("발사체 생성 위치 (적의 손/활 위치 자식 GameObject)")]
    [SerializeField] private Transform _fireOrigin;

    [Tooltip("발사체가 맞출 대상 Layer (보통 Player)")]
    [SerializeField] private LayerMask _targetLayer;

    [Header("Target")]
    [Tooltip("조준할 대상 (보통 Player). 발사 방향 계산에 사용")]
    [SerializeField] private Transform _target;

    // === 발사 방향 고정 (발사 시작 시점 캡처) ===
    // FireOnce 시점에 LockAim 으로 방향 확정 → PerformHit(릴리즈)이 그 방향으로 발사.
    // EliteChargeState 의 _chargeDirection 과 동일 철학 (직선, 회피 보상).
    private Vector3 _lockedDirection;
    private bool _hasLockedAim;

    private void Awake()
    {
        if (_config == null)
        {
            Debug.LogError($"[RangedEnemyAttacker] RangedEnemyConfig not assigned on {gameObject.name}!");
        }

        if (_fireOrigin == null)
        {
            Debug.LogError($"[RangedEnemyAttacker] FireOrigin not assigned on {gameObject.name}!");
        }

        if (_target == null)
        {
            Debug.LogWarning($"[RangedEnemyAttacker] Target not assigned on {gameObject.name}!");
        }
    }

    // ========================================================================
    // === IEnemyAttacker 구현 ===
    // ========================================================================

    /// <summary>
    /// 공격 타격 시점에 호출 (Animation Event 가 발사).
    /// Target 방향으로 발사체 1개 생성 + 데이터 주입.
    /// </summary>
    public void PerformHit()
    {
        if (_config == null || _fireOrigin == null || _target == null) return;
        if (_config.ProjectilePrefab == null)
        {
            Debug.LogError($"[RangedEnemyAttacker] ProjectilePrefab not set in Config on {gameObject.name}!");
            return;
        }

        // 1. 락된 방향이 있으면 그걸 사용(발사 시작 시점 고정), 없으면 현재 위치로 live 계산(폴백)
        Vector3 direction;
        if (_hasLockedAim)
        {
            direction = _lockedDirection;  // 이미 수평·정규화됨
        }
        else
        {
            direction = _target.position - _fireOrigin.position;
            direction.y = 0;
            if (direction.sqrMagnitude < 0.01f) return;
            direction.Normalize();
        }

        // 2. 발사체 생성 (방향 바라보도록 회전)
        GameObject projObj = Instantiate(
            _config.ProjectilePrefab,
            _fireOrigin.position,
            Quaternion.LookRotation(direction)
        );

        // 3. 발사체 데이터 주입
        if (projObj.TryGetComponent<Projectile>(out var projectile))
        {
            projectile.Initialize(
                _config.ProjectileSpeed,
                _config.Damage,
                _targetLayer,
                gameObject  // owner (발사한 적)
            );
        }
        else
        {
            Debug.LogError($"[RangedEnemyAttacker] ProjectilePrefab has no Projectile component on {gameObject.name}!");
            Destroy(projObj);
        }
    }

    /// <summary>
    /// 발사 시작 시점에 발사 방향을 1회 고정. RangedEnemyAttackState.FireOnce 가 호출.
    /// 이후 PerformHit(릴리즈)이 이 고정 방향을 쓴다 → 윈드업 중 플레이어가 옆으로 피할 여지.
    /// 너무 가까워 방향 계산 불가 시 락 해제(PerformHit 의 live 폴백).
    /// </summary>
    public void LockAim()
    {
        if (_target == null || _fireOrigin == null) { _hasLockedAim = false; return; }

        Vector3 dir = _target.position - _fireOrigin.position;
        dir.y = 0;  // 수평

        if (dir.sqrMagnitude < 0.01f) { _hasLockedAim = false; return; }

        _lockedDirection = dir.normalized;
        _hasLockedAim = true;
    }

    // ========================================================================
    // === Editor Visualization ===
    // ========================================================================

    private void OnDrawGizmosSelected()
    {
        if (_fireOrigin == null) return;

        // 발사 위치 (작은 구체)
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(_fireOrigin.position, 0.2f);

        // 발사 방향 (Target 향함)
        if (_target != null)
        {
            Vector3 direction = _target.position - _fireOrigin.position;
            direction.y = 0;
            if (direction.sqrMagnitude > 0.01f)
            {
                Gizmos.color = Color.red;
                Gizmos.DrawRay(_fireOrigin.position, direction.normalized * 3f);
            }
        }
    }
}