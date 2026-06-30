using UnityEngine;

/// <summary>
/// 거울 듀얼리스트 보스. 베이스 인프라(체력/이동/시야/쿨다운) 위에
/// "중립 허브(Neutral)" 결정 구조를 얹는다. 안개 통과 전 Dormant 로 대기,
/// BossGate.Activate() 로 기상. 행동(콤보/대시/활/패리)은 스포크로 단계 추가.
/// </summary>
public class BossStateMachine : EnemyStateMachineBase
{
    // === 상태 (스포크는 단계별 추가) ===
    public BossDormantState DormantState { get; private set; }
    public BossNeutralState NeutralState { get; private set; }
    public BossRecoveryState RecoveryState { get; private set; }
    public BossBowState BowState { get; private set; }
    public BossKickState KickState { get; private set; }
    public BossSlashState SlashState { get; private set; }
    public BossGuardState GuardState { get; private set; }
    public EnemyDeathState DeathState { get; private set; }

    // === WeaponVisual ===
    private BossWeaponVisual _weaponVisual;
    /// <summary>동작별 무기 비주얼(검/활/방패 토글). 상태가 OnEnter/OnExit 에서 사용.</summary>
    public BossWeaponVisual WeaponVisual =>
        _weaponVisual != null ? _weaponVisual : (_weaponVisual = GetComponent<BossWeaponVisual>());

    // === 페이즈 ===
    private BossConfig _bossConfig;
    private int _currentPhase = 1;
    public int CurrentPhase => _currentPhase;
    // === 페이즈2 공격성 배수 (페이즈1 = 1.0, 페이즈2 = config 배수) ===
    private float CooldownScale =>
        _currentPhase >= 2 && _bossConfig != null ? _bossConfig.Phase2CooldownMultiplier : 1f;
    public float SpeedScale =>
        _currentPhase >= 2 && _bossConfig != null ? _bossConfig.Phase2SpeedMultiplier : 1f;

    protected override void CreateStates()
    {
        _bossConfig = _config as BossConfig;
        if (_bossConfig == null)
        {
            Debug.LogError($"[BossStateMachine] requires BossConfig on {gameObject.name}! " +
                           $"Assign a BossConfig asset, not a plain EnemyConfig.");
        }

        DormantState = new BossDormantState(this);
        NeutralState = new BossNeutralState(this, _bossConfig);  
        RecoveryState = new BossRecoveryState(this);
        BowState = new BossBowState(this, _bossConfig);        
        KickState = new BossKickState(this, _bossConfig);
        SlashState = new BossSlashState(this, _bossConfig);
        GuardState = new BossGuardState(this, _bossConfig);
        DeathState = new EnemyDeathState(this);
    }

    /// <summary>안개 통과 전까지 휴면 상태로 시작 (순찰 안 함).</summary>
    protected override EnemyStateBase GetInitialState() => DormantState;

    protected override void HandleDeath() => ChangeState(DeathState);

    protected override void HandleDamaged()
    {
        CheckPhaseTransition();
        // 휴면 중 피격 시에도 기상 (안전망 - 보통은 게이트가 깨움)
        if (CurrentState == DormantState) ChangeState(NeutralState);
    }

    // === 외부(BossGate)가 호출: 휴면 → 전투 ===
    [ContextMenu("Test")]
    public void Activate()
    {
        if (CurrentState != DormantState) return;
        StartCombatCooldowns();   // 전투 진입 쿨다운 (첫 수 즉발 방지)
        ChangeState(NeutralState);
    }

    // === 전이 의도 ===
    public void ToNeutral() => ChangeState(NeutralState);

    /// <summary>행동 후 짧은 회복(빈틈) → Neutral. 회복 시간은 행동이 지정.</summary>
    public void ToRecovery(float recoverySeconds)
    {
        RecoveryState.SetRecoveryDuration(recoverySeconds);
        ChangeState(RecoveryState);
    }

    // === 활 쿨다운 (Time.time 기준, 일반 공격 쿨다운과 별개) ===
    private float _nextBowTime;
    public bool IsBowReady => Time.time >= _nextBowTime;
    public void StartBowCooldown() =>
        _nextBowTime = Time.time + (_bossConfig != null ? _bossConfig.BowCooldown : 0f) * CooldownScale;

    public void ToBow() => ChangeState(BowState);
    public void ToKick() => ChangeState(KickState);
    public void ToSlash() => ChangeState(SlashState);

    // === 가드 쿨다운 (Time.time 기준, 페이즈2에서 단축) ===
    private float _nextGuardTime;
    public bool IsGuardReady => Time.time >= _nextGuardTime;
    public void StartGuardCooldown() =>
        _nextGuardTime = Time.time + (_bossConfig != null ? _bossConfig.GuardCooldown : 0f) * CooldownScale;

    public void ToGuard()
    {
        StartGuardCooldown();   // 가드 진입 시 쿨다운 - 너무 자주 막지 않게
        ChangeState(GuardState);
    }

    /// <summary>
    /// 근접 공격 후 호출. 페이즈 배수 적용한 쿨다운으로 _nextAttackTime 갱신.
    /// 베이스 StartAttackCooldown 은 배수 미적용 - 보스 근접 패턴은 이걸 사용.
    /// (_nextAttackTime 이 protected 라 직접 스케일 → Neutral 의 IsAttackReady 가 자동 반영)
    /// </summary>
    public void StartScaledAttackCooldown() =>
        _nextAttackTime = Time.time + AttackCooldown * CooldownScale;

    /// <summary>전투 진입 시 공격 + 활 쿨다운 함께 건다 (어그로 직후 즉발 방지).</summary>
    public override void StartCombatCooldowns()
    {
        base.StartCombatCooldowns();   // 공격 쿨다운
        StartBowCooldown();            // 활 쿨다운
        StartGuardCooldown();          // 가드 쿨다운
    }

    // === 추상 구현 (제네릭 흐름용 - 허브 보스에선 직접 안 쓰이나 라우팅) ===
    public override void ToPatrol() => ChangeState(DormantState);
    public override void ToChase() => ChangeState(NeutralState);
    public override void ToAttack() => ChangeState(NeutralState);

    // === virtual override ===
    public override bool IsInCombat =>
        CurrentState != DormantState && CurrentState != DeathState;

    public override bool CanBeParried => false;  // 보스는 플레이어 패리에 경직 안 됨

    public override bool ParticipatesInRespawn => false;   // 보스는 휴식으로 부활 안 함

    // === 페이즈 전환 (HP 임계 이하 → 페이즈2, 1회) ===
    private void CheckPhaseTransition()
    {
        if (_currentPhase >= 2) return;
        if (_bossConfig == null || _health == null) return;

        float maxHp = _health.MaxHealth;
        if (maxHp <= 0f) return;

        if (_health.CurrentHealth / maxHp <= _bossConfig.Phase2HealthThreshold)
        {
            _currentPhase = 2;
            // 페이즈2 진입: 애니메이션 속도↑ (공격 스윙 날카롭게 + 광폭화)
            Animator.SetSpeedMultiplier(_bossConfig.Phase2AnimSpeedMultiplier);
        }
    }
}