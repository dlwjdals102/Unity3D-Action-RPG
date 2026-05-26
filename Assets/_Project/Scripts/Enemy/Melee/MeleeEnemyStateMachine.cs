using UnityEngine;

/// <summary>
/// 근접 적 (잡몹 1종 좀비) 의 상태머신.
/// EnemyStateMachineBase 를 상속하여 근접 전용 상태 (Patrol/Chase/Attack/Death) 와
/// 전환 로직만 구현한다.
/// 
/// 공통 메커니즘 (ChangeState, CanSeeTarget, 컴포넌트/Config/이벤트 구독, Gizmos) 은 베이스 담당.
/// 이 클래스는 "근접 적이 어떤 상태를 갖고, 전환 의도가 어떤 상태로 가는지" 만 정의.
/// </summary>
public class MeleeEnemyStateMachine : EnemyStateMachineBase
{
    // === State Instances (근접 전용) ===
    public EnemyPatrolState PatrolState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public MeleeEnemyAttackState AttackState { get; private set; }
    public EnemyDeathState DeathState { get; private set; }

    // ========================================================================
    // === Abstract Members 구현 ===
    // ========================================================================

    /// <summary>
    /// 근접 적의 상태 인스턴스 생성 (베이스 Awake 에서 호출).
    /// </summary>
    protected override void CreateStates()
    {
        PatrolState = new EnemyPatrolState(this);
        ChaseState = new EnemyChaseState(this);
        AttackState = new MeleeEnemyAttackState(this);
        DeathState = new EnemyDeathState(this);
    }

    /// <summary>
    /// 초기 진입 상태: 순찰.
    /// </summary>
    protected override EnemyStateBase GetInitialState() => PatrolState;

    /// <summary>
    /// 사망 이벤트 구독자: 즉시 DeathState 전환.
    /// </summary>
    protected override void HandleDeath()
    {
        ChangeState(DeathState);
    }

    /// <summary>
    /// 피격 이벤트 구독자: Patrol 중 + 감지 범위 내 피격 시 ChaseState 전환.
    /// "뒤에서 맞으면 돌아본다" - 시야각 무관, 거리만 체크 (베이스 헬퍼 활용).
    /// </summary>
    protected override void HandleDamaged()
    {
        // Patrol 중에만 반응 (Chase/Attack 중은 이미 추격/공격, Death 는 사망)
        if (CurrentState != PatrolState) return;

        // 감지 범위 내 피격 시만 추격 (시야각 무관, 거리만)
        if (DistanceToTarget() <= DetectionRange)
        {
            ChangeState(ChaseState);
        }
    }

    // ========================================================================
    // === State Transition Intents 구현 ===
    // 상태가 호출하는 전환 의도를 근접 상태로 매핑.
    // ========================================================================

    public override void ToPatrol() => ChangeState(PatrolState);
    public override void ToChase() => ChangeState(ChaseState);
    public override void ToAttack() => ChangeState(AttackState);
}