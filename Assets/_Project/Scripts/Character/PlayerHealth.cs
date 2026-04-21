using System;
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
    [SerializeField] private float _baseMaxHp = 100f;

    // ════════════════════════════════════════════════════
    //  IDamageable 구현
    // ════════════════════════════════════════════════════

    public float CurrentHp { get; private set; }

    public float MaxHp
    {
        get
        {
            if (_stats != null) return _stats.TotalMaxHp;
            return _baseMaxHp;
        }
    }

    public bool IsAlive => CurrentHp > 0f;

    /// <summary>HP 비율 (0~1, UI용)</summary>
    public float HpRatio => MaxHp > 0 ? CurrentHp / MaxHp : 0f;

    // ── 이벤트 ──
    /// <summary>데미지를 받았을 때 (현재HP, 최대HP)</summary>
    public event Action<float, float> OnHpChanged;

    /// <summary>사망 시</summary>
    public event Action OnDeath;

    // ── 참조 ──
    private PlayerStateMachine _stateMachine;
    private PlayerStats _stats;
    private PlayerController _controller;
    private InventorySystem _inventory;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        _stateMachine = GetComponent<PlayerStateMachine>();
        _stats = GetComponent<PlayerStats>();
        _controller = GetComponent<PlayerController>();
        _inventory = GetComponent<InventorySystem>();

        CurrentHp = MaxHp;
    }

    private void Start()
    {
        // 소비 아이템 사용 이벤트 구독
        if (_inventory != null)
            _inventory.OnConsumableUsed += OnConsumableUsed;
    }

    private void OnDestroy()
    {
        if (_inventory != null)
            _inventory.OnConsumableUsed -= OnConsumableUsed;
    }

    private void OnConsumableUsed(ItemData item)
    {
        if (item == null) return;

        switch (item.consumableType)
        {
            case ConsumableType.HealHp:
                Heal(item.effectAmount);
                Debug.Log($"[PlayerHealth] {item.itemName} 효과 적용: HP +{item.effectAmount}");
                break;
        }
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

        // 방어력 적용
        float reducedDamage = data.Amount;
        if (_stats != null && data.Type != Define.DamageType.True)
            reducedDamage = _stats.CalculateIncomingDamage(data.Amount);

        float actualDamage = Mathf.Min(reducedDamage, CurrentHp);
        CurrentHp -= actualDamage;

        Debug.Log(
            $"[Player] 피격! 데미지: {actualDamage:F0} (원본: {data.Amount:F0}) | " +
                $"HP: {CurrentHp:F0}/{MaxHp:F0}"
        );

        // HP 변경 이벤트
        OnHpChanged?.Invoke(CurrentHp, MaxHp);

        // 넉백 적용 (살아있을 때만)
        if (IsAlive && data.KnockbackForce > 0f && _controller != null)
        {
            Vector3 dir = data.KnockbackDirection;
            dir.y = 0f;
            _controller.SetExternalVelocity(dir.normalized * data.KnockbackForce);
        }

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

        CurrentHp = Mathf.Min(CurrentHp + amount, MaxHp);
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }

    /// <summary>HP를 최대로 회복합니다.</summary>
    public void FullHeal()
    {
        CurrentHp = MaxHp;
        OnHpChanged?.Invoke(CurrentHp, MaxHp);
    }
}