using UnityEngine;

/// <summary>
/// 복합 엘리트 적 (잡몹 3종) 의 상태머신.
/// EnemyStateMachineBase 를 상속하여 엘리트 전용 구성만 정의.
/// 
/// 상태 구성 (Phase 1: 콤보):
/// - PatrolState (EnemyPatrolState 재사용): 순찰
/// - ChaseState (EnemyChaseState 재사용): 근접 추격
/// - AttackState (EliteComboAttackState 신규): 확률적 콤보 공격
/// - DeathState (EnemyDeathState 재사용): 사망
/// 
/// (Phase 2 에서 ChargeState 돌진 추가, Phase 3 에서 콤보/돌진 패턴 전환 예정)
/// 
/// 근접 EnemyStateMachine 과의 차이: AttackState 가 EliteComboAttackState.
/// EliteEnemyConfig 필수 (콤보 데미지/확률). EliteComboAttackState 에 Config 주입.
/// </summary>
public class EliteEnemyStateMachine : EnemyStateMachineBase
{
    // === State Instances ===
    public EnemyPatrolState PatrolState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EliteComboAttackState AttackState { get; private set; }
    public EnemyDeathState DeathState { get; private set; }

    // ========================================================================
    // === Abstract Members 구현 ===
    // ========================================================================

    /// <summary>
    /// 엘리트 적의 상태 인스턴스 생성 (베이스 Awake 에서 호출).
    /// EliteComboAttackState 는 EliteEnemyConfig 의 콤보 데이터 필요.
    /// </summary>
    protected override void CreateStates()
    {
        PatrolState = new EnemyPatrolState(this);
        ChaseState = new EnemyChaseState(this);
        DeathState = new EnemyDeathState(this);

        // EliteComboAttackState 는 EliteEnemyConfig 의 콤보 데미지/확률 필요.
        // 베이스 _config 는 EnemyConfig 타입이라 안전 캐스팅.
        var eliteConfig = _config as EliteEnemyConfig;
        if (eliteConfig == null)
        {
            Debug.LogError($"[EliteEnemyStateMachine] requires EliteEnemyConfig on {gameObject.name}! " +
                           $"Assign an EliteEnemyConfig asset, not a plain EnemyConfig.");
            // null 로 폴백 (State 내부에서 _eliteConfig null 가드 필요 - 검증 단계에서 확인)
            AttackState = new EliteComboAttackState(this, null);
            return;
        }

        AttackState = new EliteComboAttackState(this, eliteConfig);
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
    /// 근접/원거리 StateMachine 과 동일 로직 (미래 베이스 추출 후보).
    /// </summary>
    protected override void HandleDamaged()
    {
        if (CurrentState != PatrolState) return;

        if (DistanceToTarget() <= DetectionRange)
        {
            ChangeState(ChaseState);
        }
    }

    // ========================================================================
    // === State Transition Intents 구현 ===
    // ToAttack 만 다름 (EliteComboAttackState 로).
    // ========================================================================

    public override void ToPatrol() => ChangeState(PatrolState);
    public override void ToChase() => ChangeState(ChaseState);
    public override void ToAttack() => ChangeState(AttackState);
}