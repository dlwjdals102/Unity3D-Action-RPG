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
    private PlayerStats _playerStats;  // 방어력 (공/방 스탯)

    [Header("Damage Text")]
    [Tooltip("데미지 텍스트가 표시될 머리 위 높이")]
    [SerializeField] private float _damageTextHeight = 1.8f;

    [Header("Guard")]
    [Tooltip("가드 성공 시 데미지 감소율 (0.8 = 80% 감소)")]
    [SerializeField] private float _guardDamageReduction = 0.8f;

    [Tooltip("가드로 막을 때마다 소모되는 스태미나")]
    [SerializeField] private float _guardStaminaCostPerHit = 20f;

    [Tooltip("패리 성공 시 적 경직 시간(초)")]
    [SerializeField] private float _parryStunDuration = 1.5f;

    private PlayerStateMachine _stateMachine;
    private PlayerStamina _stamina;

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

        _playerStats = GetComponent<PlayerStats>();
        _stateMachine = GetComponent<PlayerStateMachine>();
        _stamina = GetComponent<PlayerStamina>();

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

        // === 가드/패리 판정 (가드 상태 + 정면 공격일 때만) ===
        if (_stateMachine != null &&
            _stateMachine.CurrentState is GuardState guard &&
            IsAttackFromFront(info))
        {
            // 1. 패리 (가드 진입 직후 윈도우): 데미지 0 + 적 경직
            if (guard.IsParryWindow)
            {
                var attacker = info.Source != null
                    ? info.Source.GetComponentInParent<EnemyStateMachineBase>()
                    : null;

                if (attacker != null && attacker.CanBeParried)
                {
                    Debug.Log("[패리] 성공!");
                    attacker.EnterParriedStun(_parryStunDuration);
                    return;  // 데미지 0
                }
                // 패리 불가 대상(보스 등): 아래 일반 가드로 처리
                //  - 보스에게 저스트 가드가 "공짜 무효화"가 되는 밸런스 누수 방지
            }

            // 2. 일반 가드: 스태미나를 소모하며 데미지 대폭 감소
            if (_stamina != null && _stamina.TryConsume(_guardStaminaCostPerHit))
            {
                int guarded = Mathf.Max(1, Mathf.RoundToInt(info.Amount * (1f - _guardDamageReduction)));
                ApplyDamage(guarded);
                return;
            }

            // 3. 스태미나 부족: 가드 브레이크 (가드 풀리고 아래 일반 피격으로)
            Debug.Log("[가드] 브레이크! (스태미나 부족)");
            _stateMachine.ChangeState(_stateMachine.IdleState);
        }

        // 방어력만큼 데미지 감소 (최소 1은 받음)
        int defense = _playerStats != null ? _playerStats.Defense : 0;
        int finalDamage = Mathf.Max(1, info.Amount - defense);

        ApplyDamage(finalDamage);
    }

    /// <summary>실제 데미지 적용 + 공통 후처리 (HUD/텍스트/사망). 가드/일반 피격이 공용.</summary>
    private void ApplyDamage(int damage)
    {
        _currentHealth -= damage;
        if (_currentHealth < 0) _currentHealth = 0;

        // 체력 변경 알림 (HUD 갱신)
        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);

        // 머리 위에 데미지 텍스트 생성
        Vector3 textPosition = transform.position + Vector3.up * _damageTextHeight;
        DamageTextManager.Instance?.Spawn(damage, textPosition);

        // 사망 처리: 이벤트 발행 (상태머신이 DeathState 전환, 리스폰 시스템이 부활 처리)
        if (IsDead)
        {
            OnDeath?.Invoke();
        }
    }

    /// <summary>공격이 정면에서 왔는지 (가드는 정면만 유효). 출처 불명이면 정면 간주.</summary>
    private bool IsAttackFromFront(DamageInfo info)
    {
        if (info.Source == null) return true;

        Vector3 toAttacker = info.Source.transform.position - transform.position;
        toAttacker.y = 0f;  // 수평면 기준
        if (toAttacker.sqrMagnitude < 0.0001f) return true;

        return Vector3.Dot(transform.forward, toAttacker.normalized) > 0f;
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
    /// 지정량 회복 (최대 체력 초과 안 함). 물약 등 소비 아이템에 사용.
    /// 실제로 회복했으면 true. 사망/가득이면 false (호출자가 소비 여부 판단).
    /// </summary>
    public bool Heal(int amount)
    {
        if (amount <= 0) return false;
        if (IsDead) return false;  // 사망 상태는 ResetHealth 로만 복구
        if (_currentHealth >= MaxHealth) return false;  // 가득 - 물약 낭비 방지

        _currentHealth += amount;
        if (_currentHealth > MaxHealth) _currentHealth = MaxHealth;

        OnHealthChanged?.Invoke(_currentHealth, MaxHealth);
        return true;
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