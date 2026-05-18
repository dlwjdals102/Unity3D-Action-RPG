using UnityEngine;

/// <summary>
/// 플레이어의 공격 처리 컴포넌트.
/// 단일 책임: 공격 시 적 감지 + 데미지 전달 + 콤보 비용 (데미지/스태미나) 보유.
/// OverlapSphere 한 프레임 검사 방식 (단검의 순간 타격에 적합).
/// </summary>
public class PlayerAttacker : MonoBehaviour
{
    [Header("Hit Detection")]
    [SerializeField] private Transform _hitOrigin;
    [SerializeField] private float _hitRadius = 1f;
    [SerializeField] private LayerMask _enemyLayer;

    [Header("Combo Damage")]
    [SerializeField] private int[] _comboDamages = { 10, 15, 25 };

    [Header("Combo Stamina Cost")]
    [SerializeField] private float[] _comboStaminaCosts = { 15f, 20f, 25f };

    private int _currentDamage;

    private void Awake()
    {
        if (_hitOrigin == null)
        {
            Debug.LogError("[PlayerAttacker] HitOrigin not assigned! Please assign in Inspector.");
        }
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
        if (comboIndex < 0 || comboIndex >= _comboDamages.Length)
        {
            Debug.LogWarning($"[PlayerAttacker] Invalid combo index: {comboIndex}");
            return;
        }

        _currentDamage = _comboDamages[comboIndex];
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