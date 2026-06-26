using UnityEngine;

/// <summary>
/// 보스 휴면 상태. 안개 통과 전까지 가만히 대기. BossGate.Activate() 가 Neutral 로 깨운다.
/// </summary>
public class BossDormantState : EnemyStateBase
{
    public BossDormantState(EnemyStateMachineBase stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        _stateMachine.Movement.StopMoving();
        _stateMachine.Animator.SetMoveSpeed(0f);
        _stateMachine.Animator.PlayIdle();
    }

    public override void OnUpdate() { }  // 깨어날 때까지 아무것도 안 함
}