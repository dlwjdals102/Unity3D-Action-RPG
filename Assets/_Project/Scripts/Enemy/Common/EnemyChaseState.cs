using UnityEngine;

/// <summary>
/// 적의 추격 상태.
/// Target (플레이어) 의 현재 위치로 매 프레임 이동한다.
/// ChaseSpeed (Run 속도) + Animator MoveSpeed 는 실제 이동 속도 기반.
/// 
/// 상태 전환:
/// - 공격 거리 도달 시 → MeleeEnemyAttackState
/// - 추격 포기 거리 초과 시 → EnemyPatrolState (Target 이 멀어짐)
/// </summary>
public class EnemyChaseState : EnemyStateBase
{
    // 추격 정지 거리. 공격 거리에서 멈추는 건 CanAttackTarget→ToAttack 가 담당하므로,
    // 여기선 작게 둬서 시야가 막히면 NavMesh 가 벽을 끝까지 우회해 접근하게 한다.
    private const float StoppingDistance = 0.5f;

    public EnemyChaseState(EnemyStateMachineBase stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // 추격은 공격 거리(AttackRange)에서 멈춤. NavMeshAgent 가 그 거리에서 자동 정지.
        // (Patrol 의 0 에서 복원 - 각 상태가 자기 stoppingDistance 명시)
        _stateMachine.Movement.SetStoppingDistance(StoppingDistance);

        // Animator 를 Locomotion 으로 명시적 전환
        _stateMachine.Animator.PlayLocomotion();
    }

    public override void OnUpdate()
    {
        // Target null 안전망
        if (_stateMachine.Target == null) return;

        // 1. 공격 가능(사거리 + 시야)이면 공격 전환.
        //    진입과 유지가 같은 술어(CanAttackTarget)를 쓴다 → 비대칭 구멍 불가능.
        if (_stateMachine.CanAttackTarget())
        {
            _stateMachine.ToAttack();
            return;
        }

        // 2. 추격 포기 거리 초과 → 순찰 복귀
        float distance = _stateMachine.Movement.DistanceTo(_stateMachine.Target.position);
        if (distance > _stateMachine.GiveUpRange)
        {
            _stateMachine.ToPatrol();
            return;
        }

        // 3. Target 위치로 매 프레임 이동 (NavMeshAgent 가 벽 우회 경로 재계산)
        _stateMachine.Movement.MoveTo(
            _stateMachine.Target.position,
            _stateMachine.Movement.ChaseSpeed
        );

        // 4. Animator MoveSpeed 갱신
        UpdateAnimatorMoveSpeed();
    }

    /// <summary>
    /// NavMeshAgent 의 실제 속도를 ChaseSpeed 로 정규화하여 Animator 에 전달.
    /// 실제 속도 0 → MoveSpeed 0 (Idle), 실제 속도 ChaseSpeed → MoveSpeed 1 (Run).
    /// </summary>
    private void UpdateAnimatorMoveSpeed()
    {
        float actualSpeed = _stateMachine.Movement.CurrentSpeed;
        float normalizedSpeed = Mathf.Clamp01(actualSpeed / _stateMachine.Movement.ChaseSpeed);
        _stateMachine.Animator.SetMoveSpeed(normalizedSpeed);
    }
}