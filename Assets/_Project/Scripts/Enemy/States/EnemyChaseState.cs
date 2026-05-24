using UnityEngine;

/// <summary>
/// 적의 추격 상태.
/// Target (플레이어) 의 현재 위치로 매 프레임 이동한다.
/// ChaseSpeed (Run 속도) + Animator MoveSpeed 는 실제 이동 속도 기반.
/// 
/// 상태 전환:
/// - 공격 거리 도달 시 → EnemyAttackState
/// - 추격 포기 거리 초과 시 → EnemyPatrolState (Target 이 멀어짐)
/// </summary>
public class EnemyChaseState : EnemyStateBase
{
    public EnemyChaseState(EnemyStateMachineBase stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // 추격은 공격 거리(AttackRange)에서 멈춤. NavMeshAgent 가 그 거리에서 자동 정지.
        // (Patrol 의 0 에서 복원 - 각 상태가 자기 stoppingDistance 명시)
        _stateMachine.Movement.SetStoppingDistance(_stateMachine.AttackRange);

        // Animator 를 Locomotion 으로 명시적 전환
        _stateMachine.Animator.PlayLocomotion();
    }

    public override void OnUpdate()
    {
        // Target null 안전망 (Inspector 할당 누락 시)
        if (_stateMachine.Target == null) return;

        float distance = _stateMachine.Movement.DistanceTo(_stateMachine.Target.position);

        // 1. 공격 거리 도달 시 → 공격 전환 (파생이 대상 결정)
        if (distance < _stateMachine.AttackRange)
        {
            _stateMachine.ToAttack();
            return;
        }

        // 2. 추격 포기 거리 초과 시 → 순찰 복귀 (파생이 대상 결정)
        if (distance > _stateMachine.GiveUpRange)
        {
            _stateMachine.ToPatrol();
            return;
        }

        // 3. Target 위치로 매 프레임 이동 (NavMeshAgent 가 경로 재계산)
        _stateMachine.Movement.MoveTo(
            _stateMachine.Target.position,
            _stateMachine.Movement.ChaseSpeed
        );

        // 4. Animator MoveSpeed 를 실제 속도 기반으로 갱신
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