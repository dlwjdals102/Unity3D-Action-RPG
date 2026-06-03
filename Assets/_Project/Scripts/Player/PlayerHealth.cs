using UnityEngine;
using System;
using UnityEngine.InputSystem;

/// <summary>
/// 플레이어의 체력 관리 + IDamageable 구현.
/// 단일 책임: 체력 추적 + 데미지 받기 + 무적 상태 관리.
/// 무적 상태 시 데미지 무시 (회피 시 짧은 무적의 본질).
/// 사망 처리는 지금은 로그만, 미래 (Week 9 화톳불 단계) 에 본격 구현 예정.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    [Header("Stats")]
    [Tooltip("플레이어 정적 스탯 (최대 체력 등). PlayerStatsConfig 에셋 할당")]
    [SerializeField] private PlayerStatsConfig _stats;

    [Header("Damage Text")]
    [Tooltip("데미지 텍스트가 표시될 머리 위 높이")]
    [SerializeField] private float _damageTextHeight = 1.8f;

    private int _currentHealth;
    private bool _isInvincible;

    /// <summary>체력 변경 시 발행. (현재 체력, 최대 체력). HUD 등이 구독.</summary>
    public event Action<int, int> OnHealthChanged;

    /// <summary>사망 시 1회 발행 (상태머신의 DeathState 전환 + 리스폰 시스템이 구독).</summary>
    public event Action OnDeath;

    // === Public Properties ===
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _stats != null ? _stats.MaxHealth : 100;
    public bool IsDead => _currentHealth <= 0;
    public bool IsInvincible => _isInvincible;

    private void Awake()
    {
        if (_stats == null)
        {
            Debug.LogError($"[PlayerHealth] PlayerStatsConfig not assigned on {gameObject.name}!");
        }

        _currentHealth = MaxHealth;
    }

    private void Start()
    {
        // 초기 체력을 HUD 등에 알림 (구독자가 Awake 이후 등록될 수 있어 Start 에서 발행)
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
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

        // 체력 변경 알림 (HUD 갱신)
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);

        // 머리 위에 데미지 텍스트 생성
        Vector3 textPosition = transform.position + Vector3.up * _damageTextHeight;
        DamageTextManager.Instance?.Spawn(info.Amount, textPosition);

        // 사망 처리: 이벤트 발행 (상태머신이 DeathState 전환, 리스폰 시스템이 부활 처리)
        if (IsDead)
        {
            OnDeath?.Invoke();
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

    /// <summary>
    /// 지정량 회복 (최대 체력 초과 안 함). 향후 물약 등에 사용.
    /// </summary>
    public void Heal(int amount)
    {
        if (amount <= 0) return;
        if (IsDead) return;  // 사망 상태는 ResetHealth 로만 복구

        _currentHealth += amount;
        if (_currentHealth > MaxHealth) _currentHealth = MaxHealth;

        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
    }

    /// <summary>
    /// 최대 체력으로 완전 회복 (부활/화톳불 휴식). 사망 상태도 복구.
    /// </summary>
    public void ResetHealth()
    {
        _currentHealth = MaxHealth;
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
    }

    // 임시 테스트용: K 키로 즉사. 리스폰 검증 후 제거.
    private void Update()
    {
        if (Keyboard.current != null && Keyboard.current.kKey.wasPressedThisFrame)
        {
            var info = new DamageInfo
            {
                Amount = 50,
                Source = gameObject,
                HitPoint = transform.position
            };
            TakeDamage(info);
        }
    }
}