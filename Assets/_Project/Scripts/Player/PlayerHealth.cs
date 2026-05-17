using UnityEngine;

/// <summary>
/// 플레이어의 체력 관리 + IDamageable 구현.
/// 단일 책임: 체력 추적 + 데미지 받기.
/// 사망 처리는 지금은 로그만, 미래 (Week 9 화톳불 단계) 에 본격 구현 예정.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 100;

    private int _currentHealth;

    // === Public Properties ===
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsDead => _currentHealth <= 0;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    /// <summary>
    /// IDamageable 인터페이스 구현.
    /// 양수 데미지만 처리하며, 이미 사망 상태면 무시한다.
    /// </summary>
    public void TakeDamage(DamageInfo info)
    {
        // 이미 사망한 상태면 무시
        if (IsDead) return;

        // 양수 데미지만 처리 (회복은 별도 메서드로 처리 예정)
        if (info.Amount <= 0) return;

        // 체력 감소 (0 미만 방지)
        _currentHealth -= info.Amount;
        if (_currentHealth < 0) _currentHealth = 0;

        Debug.Log($"[PlayerHealth] Took {info.Amount} damage. HP: {_currentHealth}/{_maxHealth}");

        // 사망 처리 (지금은 로그만, 미래 확장 예정)
        if (IsDead)
        {
            Debug.Log("[PlayerHealth] Player died!");
        }
    }
}