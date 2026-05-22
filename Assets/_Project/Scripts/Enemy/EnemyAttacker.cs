using UnityEngine;

/// <summary>
/// 적의 공격 처리 컴포넌트.
/// PlayerAttacker 패턴 일관: Animation Event 시점에 OverlapSphere 한 프레임 검사로 타격 처리.
/// 단순 잡몹: 단일 데미지 (콤보 없음), 스태미나 없음 (무한 공격).
/// 미래 (엘리트/보스): 콤보 별 데미지 배열, 스태미나 시스템 도입 가능.
/// 
/// 단일 책임: 공격 시 타격 감지 + 데미지 전달.
/// </summary>
public class EnemyAttacker : MonoBehaviour
{
    [Header("Hit Detection")]
    [Tooltip("타격 감지 영역의 중심 (적의 앞쪽 자식 GameObject)")]
    [SerializeField] private Transform _hitOrigin;

    [Tooltip("타격 감지 반경 (m)")]
    [SerializeField] private float _hitRadius = 1f;

    [Tooltip("공격 대상 Layer (보통 Player)")]
    [SerializeField] private LayerMask _targetLayer;

    [Header("Damage")]
    [Tooltip("공격 시 적용되는 데미지")]
    [SerializeField] private int _damage = 10;

    private void Awake()
    {
        if (_hitOrigin == null)
        {
            Debug.LogError($"[EnemyAttacker] HitOrigin not assigned on {gameObject.name}! Please assign in Inspector.");
        }
    }

    // ========================================================================
    // === Public API ===
    // ========================================================================

    /// <summary>
    /// 공격 타격 시점에 호출 (Animation Event 가 발사).
    /// OverlapSphere 한 프레임 검사로 영역 내 대상 감지 + 데미지 적용.
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
                    Amount = _damage,
                    Source = gameObject,
                    HitPoint = hit.ClosestPoint(_hitOrigin.position)
                };

                target.TakeDamage(info);
            }
        }
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