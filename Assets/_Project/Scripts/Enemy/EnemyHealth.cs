using System;
using UnityEngine;

/// <summary>
/// 적의 체력 관리 + IDamageable 구현.
/// 단일 책임: 체력 추적 + 데미지 받기 + 피격/사망 알림 (이벤트 발행).
/// 
/// 사망/피격 처리는 이벤트로 외부에 알리고, 실제 처리 (Death 애니메이션, 추격 전환 등) 는
/// 구독자 (EnemyStateMachine) 가 담당. 느슨한 결합 패턴.
/// 
/// 미래 구독자 예시:
/// - LockOnManager (락온 자동 해제)
/// - UI (적 처치 카운트, 영혼 드롭 알림)
/// - SoundManager (사망/피격 효과음)
/// </summary>
public class EnemyHealth : MonoBehaviour, IDamageable
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 50;

    [Header("Damage Text")]
    [Tooltip("데미지 텍스트가 표시될 머리 위 높이")]
    [SerializeField] private float _damageTextHeight = 1.8f;

    private int _currentHealth;

    // === Public Properties ===
    public int CurrentHealth => _currentHealth;
    public int MaxHealth => _maxHealth;
    public bool IsDead => _currentHealth <= 0;

    // === Events ===
    /// <summary>피격 시점에 발생 (사망이 아닐 때만). 구독자가 추격 전환 등 처리.</summary>
    public event Action OnDamaged;

    /// <summary>사망 시점에 1회 발생. 여러 구독자 (StateMachine, LockOn, UI 등) 가 구독 가능.</summary>
    public event Action OnDeath;

    private void Awake()
    {
        _currentHealth = _maxHealth;
    }

    /// <summary>
    /// IDamageable 인터페이스 구현.
    /// 양수 데미지만 처리하며, 이미 사망 상태면 무시한다.
    /// 사망 시 OnDeath, 사망이 아닌 피격 시 OnDamaged 이벤트 발행.
    /// </summary>
    public void TakeDamage(DamageInfo info)
    {
        if (IsDead) return;
        if (info.Amount <= 0) return;

        _currentHealth -= info.Amount;
        if (_currentHealth < 0) _currentHealth = 0;

        // 머리 위에 데미지 텍스트 생성
        Vector3 textPosition = transform.position + Vector3.up * _damageTextHeight;
        DamageTextManager.Instance?.Spawn(info.Amount, textPosition);

        // 사망 vs 피격 분기: 사망 시 OnDeath, 아니면 OnDamaged
        if (IsDead)
        {
            Debug.Log($"[EnemyHealth] {gameObject.name} died!");
            OnDeath?.Invoke();
        }
        else
        {
            OnDamaged?.Invoke();
        }
    }
}