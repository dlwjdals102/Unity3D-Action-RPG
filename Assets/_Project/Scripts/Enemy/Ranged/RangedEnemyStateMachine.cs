using UnityEngine;

/// <summary>
/// 원거리 적 (잡몹 2종 궁수/마법사) 의 상태머신.
/// EnemyStateMachineBase 를 상속하여 원거리 전용 구성만 정의.
/// 
/// 상태 구성:
/// - PatrolState (EnemyPatrolState 재사용): 순찰
/// - ChaseState (EnemyChaseState 재사용): 접근 (제자리 발사라 근접 추격과 동일 로직)
/// - AttackState (RangedAttackState 신규): 발사 + 쿨다운
/// - DeathState (EnemyDeathState 재사용): 사망
/// 
/// 근접 MeleeEnemyStateMachine 과의 유일한 차이: AttackState 가 RangedAttackState.
/// 전환 의도 ToAttack 이 RangedAttackState 로 감 (PatrolState/ChaseState 가 공통이어도
/// 원거리에선 발사 상태로 전환).
/// 
/// RangedEnemyConfig 필수 (발사체 데이터 + 쿨다운). Inspector 에서 할당.
/// </summary>
public class RangedEnemyStateMachine : EnemyStateMachineBase
{
    // === State Instances ===
    public EnemyPatrolState PatrolState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public RangedEnemyAttackState AttackState { get; private set; }
    public EnemyDeathState DeathState { get; private set; }

    // ========================================================================
    // === Abstract Members 구현 ===
    // ========================================================================

    /// <summary>
    /// 원거리 적의 상태 인스턴스 생성 (베이스 Awake 에서 호출).
    /// RangedAttackState 는 Config 의 쿨다운을 주입받는다.
    /// </summary>
    protected override void CreateStates()
    {
        PatrolState = new EnemyPatrolState(this);
        ChaseState = new EnemyChaseState(this);
        DeathState = new EnemyDeathState(this);
        AttackState = new RangedEnemyAttackState(this);
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
    /// 근접 MeleeEnemyStateMachine 과 동일 로직 (미래 베이스 추출 후보).
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
    // ToAttack 만 근접과 다름 (RangedAttackState 로).
    // ========================================================================

    public override void ToPatrol() => ChangeState(PatrolState);
    public override void ToChase() => ChangeState(ChaseState);
    public override void ToAttack() => ChangeState(AttackState);
}