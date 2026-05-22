using UnityEngine;

/// <summary>
/// 플레이어의 체력 관리 + IDamageable 구현.
/// 단일 책임: 체력 추적 + 데미지 받기 + 무적 상태 관리.
/// 무적 상태 시 데미지 무시 (회피 시 짧은 무적의 본질).
/// 사망 처리는 지금은 로그만, 미래 (Week 9 화톳불 단계) 에 본격 구현 예정.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 100;

    [Header("Damage Text")]
    [Tooltip("데미지 텍스트가 표시될 머리 위 높이")]
    [SerializeField] private float _damageTextHeight = 1.8f;

    private int _currentHealth;
    private bool _isInvincible;

    // === Public Properties ===
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsDead => _currentHealth <= 0;
    public bool IsInvincible => _isInvincible;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    // ========================================================================
    // === Public API ===
    // ========================================================================

    /// <summary>
    /// IDamageable 인터페이스 구현.
    /// 양수 데미지만 처리하며, 이미 사망 상태나 무적 상태면 무시한다.
    /// </summary>
    public void TakeDamage(DamageInfo info)
    {
        if (IsDead) return;
        if (_isInvincible) return;  // 무적 시 데미지 무시 (회피 무적 시간)
        if (info.Amount <= 0) return;

        _currentHealth -= info.Amount;
        if (_currentHealth < 0) _currentHealth = 0;

        // 머리 위에 데미지 텍스트 생성
        Vector3 textPosition = transform.position + Vector3.up * _damageTextHeight;
        DamageTextManager.Instance?.Spawn(info.Amount, textPosition);

        // 사망 처리 (지금은 로그만, 미래 확장 예정)
        if (IsDead)
        {
            Debug.Log("[PlayerHealth] Player died!");
        }
    }

    /// <summary>
    /// 무적 상태 설정. 회피 시 Animation Event 가 PlayerAnimationEventReceiver 를 통해 호출.
    /// true: 회피 무적 구간 시작
    /// false: 회피 무적 구간 종료 또는 Receiver 의 안전망
    /// </summary>
    public void SetInvincible(bool value)
    {
        _isInvincible = value;
    }
}