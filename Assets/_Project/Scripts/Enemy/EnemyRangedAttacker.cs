using UnityEngine;

/// <summary>
/// 원거리 적의 공격 처리. IEnemyAttacker 구현.
/// 근접 EnemyAttacker (OverlapSphere 즉시 타격) 와 달리, FireOrigin 에서 발사체를 생성한다.
/// 
/// 발사체 시각/수치 (Prefab, 속도, 데미지) 는 RangedEnemyConfig 에서 읽는다.
/// → 같은 코드 + 다른 Config = 궁수(화살) / 마법사(마법 구체).
/// 
/// PerformHit (Animation Event 호출) 시점에 Target 방향으로 발사체 1개 생성.
/// </summary>
public class EnemyRangedAttacker : MonoBehaviour, IEnemyAttacker
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

    private void Awake()
    {
        if (_config == null)
        {
            Debug.LogError($"[EnemyRangedAttacker] RangedEnemyConfig not assigned on {gameObject.name}!");
        }

        if (_fireOrigin == null)
        {
            Debug.LogError($"[EnemyRangedAttacker] FireOrigin not assigned on {gameObject.name}!");
        }

        if (_target == null)
        {
            Debug.LogWarning($"[EnemyRangedAttacker] Target not assigned on {gameObject.name}!");
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
            Debug.LogError($"[EnemyRangedAttacker] ProjectilePrefab not set in Config on {gameObject.name}!");
            return;
        }

        // 1. 발사 방향 계산 (Target 조준, 수평만)
        Vector3 direction = _target.position - _fireOrigin.position;
        direction.y = 0;  // 수평 발사 (높이 무시)

        if (direction.sqrMagnitude < 0.01f) return;  // 너무 가까움 (방향 계산 불가)
        direction.Normalize();

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
            Debug.LogError($"[EnemyRangedAttacker] ProjectilePrefab has no Projectile component on {gameObject.name}!");
            Destroy(projObj);
        }
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