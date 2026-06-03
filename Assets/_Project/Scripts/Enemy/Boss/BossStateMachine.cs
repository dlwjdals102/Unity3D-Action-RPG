using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 보스의 상태머신. EnemyStateMachineBase 를 상속 (근접/원거리/엘리트와 형제).
/// 
/// [1] 기본 골격 단계: 공통 State 를 재사용해 "움직이고 때린다" 부터 확립.
/// - PatrolState (EnemyPatrolState 재사용): 순찰
/// - ChaseState (EnemyChaseState 재사용): 추격
/// - AttackState (MeleeEnemyAttackState 재사용): 기본 근접 공격 1개
/// - DeathState (EnemyDeathState 재사용): 사망
/// 
/// 추후 단계에서 보스 전용 확장:
/// - [2] 패턴 State (콤보/돌진/범위 공격)
/// - [3] 페이즈 시스템 (HP 구간별 패턴 변화, BossConfig.Phase2HealthThreshold)
/// 
/// BossConfig 권장 (높은 체력 + 페이즈 임계값). 없어도 베이스 EnemyConfig 로 동작.
/// </summary>
public class BossStateMachine : EnemyStateMachineBase
{
    // === State Instances ===
    public EnemyPatrolState PatrolState { get; private set; }
    public BossChaseState ChaseState { get; private set; }
    public BossAttackState AttackState { get; private set; }
    public BossChargeState ChargeState { get; private set; }
    public BossSlamState SlamState { get; private set; }
    public BossCooldownState CooldownState { get; private set; }
    public EnemyDeathState DeathState { get; private set; }

    // === Phase ===
    private int _currentPhase = 1;
    private BossConfig _bossConfig;

    /// <summary>현재 페이즈 (1 또는 2). 패턴 선택기가 참조.</summary>
    public int CurrentPhase => _currentPhase;

    // === Attack Pattern Pool ===
    /// <summary>
    /// 보스 공격 패턴 정의. 선택기가 "현재 페이즈 + 거리" 로 필터 후 가중치 랜덤 선택.
    /// 패턴 추가 = 이 리스트에 항목 추가 (데이터 주도 확장).
    /// </summary>
    private class AttackPattern
    {
        public string Name;
        public int MinPhase;       // 이 페이즈 이상에서만 사용 (1 = 항상, 2 = 페이즈 2부터)
        public float Weight;       // 선택 가중치 (클수록 자주)
        public Action Execute;     // 실행 (해당 State 로 전환)
    }

    private List<AttackPattern> _patterns;
    private float _nextAttackTime;  // 이 시각 이후 다음 공격 가능 (쿨다운)

    // ========================================================================
    // === Abstract Members 구현 ===
    // ========================================================================

    /// <summary>
    /// 보스 상태 인스턴스 생성 (베이스 Awake 에서 호출).
    /// [1] 단계는 공통 State 재사용. [2]에서 보스 전용 패턴으로 교체/추가.
    /// </summary>
    protected override void CreateStates()
    {
        // BossConfig 캐스팅 (돌진 등 보스 전용 패턴이 사용). null 이면 각 State 가 가드.
        var bossConfig = _config as BossConfig;
        if (bossConfig == null)
        {
            Debug.LogError($"[BossStateMachine] requires BossConfig on {gameObject.name}! " +
                           $"Assign a BossConfig asset, not a plain EnemyConfig.");
        }
        _bossConfig = bossConfig;  // 페이즈 전환 판정에 사용

        PatrolState = new EnemyPatrolState(this);
        ChaseState = new BossChaseState(this, bossConfig);
        AttackState = new BossAttackState(this);
        ChargeState = new BossChargeState(this, bossConfig);
        SlamState = new BossSlamState(this, bossConfig);
        CooldownState = new BossCooldownState(this, bossConfig);
        DeathState = new EnemyDeathState(this);

        BuildPatternPool(bossConfig);
    }

    /// <summary>
    /// 공격 패턴 풀 구성. 패턴별 페이즈/가중치/거리 조건을 데이터로 정의.
    /// (페이즈 1: 기본 공격만 / 페이즈 2: 기본 + 돌진 + 내려찍기)
    /// </summary>
    private void BuildPatternPool(BossConfig config)
    {
        _patterns = new List<AttackPattern>();

        // 패턴 선택은 "보스가 근접(AttackRange)에 도달한 시점"에만 일어나므로,
        // 거리 필터(MinRange/MaxRange)는 사용하지 않는다. 모든 패턴이 근접에서 동등하게 후보.
        // (거리로 패턴을 거르면 "특정 거리에서 한 패턴만 후보"가 되어 그 패턴만 반복되는
        //  경계 문제가 생긴다. 근접 도달 시점으로 선택을 고정해 그 문제를 구조적으로 제거.)

        // 기본 공격: 모든 페이즈
        _patterns.Add(new AttackPattern
        {
            Name = "Attack",
            MinPhase = 1,
            Weight = 1f,
            Execute = ToAttack
        });

        if (config != null)
        {
            // 돌진: 페이즈 2 (근접에서 발동 → 관통하며 거리 리셋)
            _patterns.Add(new AttackPattern
            {
                Name = "Charge",
                MinPhase = 2,
                Weight = 1f,
                Execute = ToCharge
            });

            // 내려찍기: 페이즈 2 (근접에서 발동 → 제자리 원형)
            _patterns.Add(new AttackPattern
            {
                Name = "Slam",
                MinPhase = 2,
                Weight = 0.8f,
                Execute = ToSlam
            });
        }
    }

    /// <summary>초기 진입 상태: 순찰.</summary>
    protected override EnemyStateBase GetInitialState() => PatrolState;

    /// <summary>사망 이벤트 구독자: 즉시 DeathState 전환.</summary>
    protected override void HandleDeath()
    {
        ChangeState(DeathState);
    }

    /// <summary>
    /// 피격 이벤트 구독자: Patrol 중 + 감지 범위 내 피격 시 ChaseState 전환.
    /// (근접/원거리/엘리트와 동일 로직 - 미래 베이스 추출 후보)
    /// </summary>
    protected override void HandleDamaged()
    {
        // 페이즈 전환 체크 (피격마다 HP 비율 확인, 한 방향 전환)
        CheckPhaseTransition();

        if (CurrentState != PatrolState) return;

        if (DistanceToTarget() <= DetectionRange)
        {
            ChangeState(ChaseState);
        }
    }

    /// <summary>
    /// HP 비율이 페이즈 2 임계값 이하로 떨어지면 페이즈 2 로 전환 (1회, 되돌아가지 않음).
    /// </summary>
    private void CheckPhaseTransition()
    {
        if (_currentPhase >= 2) return;            // 이미 페이즈 2
        if (_bossConfig == null || _health == null) return;

        float maxHp = _health.MaxHealth;
        if (maxHp <= 0f) return;

        float ratio = _health.CurrentHealth / maxHp;
        if (ratio <= _bossConfig.Phase2HealthThreshold)
        {
            _currentPhase = 2;
            Debug.Log("[Boss] 페이즈 2 진입!");  // 임시 확인 (페이즈 연출은 [5] 인트로/폴리싱)
        }
    }

    // ========================================================================
    // === State Transition Intents 구현 ===
    // ========================================================================

    public override void ToPatrol() => ChangeState(PatrolState);
    public override void ToChase() => ChangeState(ChaseState);
    public override void ToAttack() => ChangeState(AttackState);
    public override bool IsInCombat =>
        CurrentState != null && CurrentState != PatrolState && CurrentState != DeathState;

    /// <summary>돌진 상태로 전환. (임시 검증용으로 직접 호출, [2-3]에서 패턴 선택기가 호출)</summary>
    public void ToCharge() => ChangeState(ChargeState);

    /// <summary>내려찍기 상태로 전환. (임시 검증용으로 직접 호출, [2-3]에서 패턴 선택기가 호출)</summary>
    public void ToSlam() => ChangeState(SlamState);

    /// <summary>쿨다운 대기 상태로 전환. 모든 패턴이 끝나면 여기로 (둠칫 없는 쿨다운).</summary>
    public void ToCooldown() => ChangeState(CooldownState);

    /// <summary>
    /// 현재 페이즈 + 거리에 맞는 패턴을 가중치 랜덤으로 골라 실행한다.
    /// 후보가 없으면 false (BossChaseState 가 계속 추격).
    /// </summary>
    /// <param name="distance">플레이어와의 현재 거리</param>
    /// <returns>패턴을 실행했으면 true</returns>
    public bool TrySelectAndExecutePattern()
    {
        // 쿨다운 체크
        if (Time.time < _nextAttackTime) return false;
        if (_patterns == null) return false;

        // 1. 현재 페이즈 조건 만족하는 후보 수집 (거리 필터 없음 - 근접 도달 시점에만 호출되므로)
        float totalWeight = 0f;
        var candidates = new List<AttackPattern>();
        foreach (var p in _patterns)
        {
            if (_currentPhase < p.MinPhase) continue;
            candidates.Add(p);
            totalWeight += p.Weight;
        }

        if (candidates.Count == 0 || totalWeight <= 0f) return false;

        // 2. 가중치 랜덤 선택
        float roll = UnityEngine.Random.value * totalWeight;
        AttackPattern chosen = candidates[candidates.Count - 1];  // 폴백 (부동소수 안전)
        float accum = 0f;
        foreach (var p in candidates)
        {
            accum += p.Weight;
            if (roll <= accum)
            {
                chosen = p;
                break;
            }
        }

        // 3. 쿨다운 시작 + 실행
        _nextAttackTime = Time.time + AttackCooldown;
        chosen.Execute();
        return true;
    }
}