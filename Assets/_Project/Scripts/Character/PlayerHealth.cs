using UnityEngine;

/// <summary>
/// 플레이어 HP 관리 및 IDamageable 구현.
/// PlayerStateMachine과 연동하여 피격/사망 상태를 전환합니다.
/// 
/// [10단계에서 확장 예정]
/// 스탯 시스템과 연동하여 방어력, 버프 등을 적용할 수 있습니다.
/// </summary>
public class PlayerHealth : MonoBehaviour, IDamageable
{
    // ════════════════════════════════════════════════════
    //  설정
    // ════════════════════════════════════════════════════

    [Header("Stats")]
    [SerializeField] private float _maxHp = 100f;

    // ════════════════════════════════════════════════════
    //  IDamageable 구현
    // ════════════════════════════════════════════════════

    public float CurrentHp { get; private set; }
    public float MaxHp => _maxHp;
    public bool IsAlive => CurrentHp > 0f;

    /// <summary>HP 비율 (0~1, UI용)</summary>
    public float HpRatio => _maxHp > 0 ? CurrentHp / _maxHp : 0f;

    // ── 이벤트 ──
    /// <summary>데미지를 받았을 때 (현재HP, 최대HP)</summary>
    public event System.Action<float, float> OnHpChanged;

    /// <summary>사망 시</summary>
    public event System.Action OnDeath;

    // ── 참조 ──
    private PlayerStateMachine _stateMachine;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        CurrentHp = _maxHp;
        _stateMachine = GetComponent<PlayerStateMachine>();
    }

    // ════════════════════════════════════════════════════
    //  IDamageable
    // ════════════════════════════════════════════════════

    public float TakeDamage(DamageData data)
    {
        if (!IsAlive) return 0f;

        // 회피 무적 체크
        if (_stateMachine != null &&
            _stateMachine.CurrentStateType == Define.CharacterState.Dodge)
            return 0f;

        float actualDamage = Mathf.Min(data.Amount, CurrentHp);
        CurrentHp -= actualDamage;

        Debug.Log(
            $"[Player] 피격! 데미지: {actualDamage:F0} | " +
            $"HP: {CurrentHp:F0}/{_maxHp:F0}"
        );

        // HP 변경 이벤트
        OnHpChanged?.Invoke(CurrentHp, _maxHp);

        // 사망 체크
        if (!IsAlive)
        {
            OnDeath?.Invoke();
            _stateMachine?.TransitionTo(Define.CharacterState.Die);
        }
        else
        {
            // 피격 상태 전환
            _stateMachine?.OnTakeDamage(actualDamage);
        }

        return actualDamage;
    }

    // ════════════════════════════════════════════════════
    //  유틸리티
    // ════════════════════════════════════════════════════

    /// <summary>HP를 회복합니다.</summary>
    public void Heal(float amount)
    {
        if (!IsAlive) return;

        CurrentHp = Mathf.Min(CurrentHp + amount, _maxHp);
        OnHpChanged?.Invoke(CurrentHp, _maxHp);
    }

    /// <summary>HP를 최대로 회복합니다.</summary>
    public void FullHeal()
    {
        CurrentHp = _maxHp;
        OnHpChanged?.Invoke(CurrentHp, _maxHp);
    }
}