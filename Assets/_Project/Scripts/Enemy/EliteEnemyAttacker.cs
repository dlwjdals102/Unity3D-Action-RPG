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