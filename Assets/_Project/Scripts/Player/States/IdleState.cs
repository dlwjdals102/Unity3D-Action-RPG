using UnityEngine;

/// <summary>
/// 플레이어 대기 상태.
/// 입력에 따라 DodgeState, AttackState, JumpState, MoveState 로 전환되며,
/// 공중 감지 시 FallState 로 자동 전환된다.
/// 진입 시 Animator 를 Locomotion 상태로 명시적 전환한다.
/// </summary>
public class IdleState : PlayerStateBase
{
    private const float MoveInputThreshold = 0.01f;

    public IdleState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // Animator 를 Locomotion 으로 명시적 전환 (Single Source of Truth)
        _stateMachine.Animator.PlayLocomotion();
    }

    public override void OnUpdate()
    {
        // 1. 공중 감지 → FallState (최우선, 절벽에서 떨어짐 케이스)
        if (!_stateMachine.Movement.IsGrounded)
        {
            _stateMachine.ChangeState(_stateMachine.FallState);
            return;
        }

        // 2. 회피 입력 → DodgeState (소울라이크 표준: 회피 최우선)
        if (_stateMachine.Controller.DodgeRequested && _stateMachine.Movement.CanDodge)
        {
            _stateMachine.ChangeState(_stateMachine.DodgeState);
            return;
        }

        // 3. 공격 입력 → AttackState (전투 게임 우선)
        if (_stateMachine.Controller.AttackRequested)
        {
            _stateMachine.ChangeState(_stateMachine.AttackState);
            return;
        }

        // 4. 점프 입력 → JumpState
        if (_stateMachine.Controller.JumpRequested)
        {
            _stateMachine.ChangeState(_stateMachine.JumpState);
            return;
        }

        // 5. 이동 입력 → MoveState
        Vector2 moveInput = _stateMachine.Controller.MoveInput;
        if (moveInput.sqrMagnitude > MoveInputThreshold)
        {
            _stateMachine.ChangeState(_stateMachine.MoveState);
            return;
        }

        // 6. 입력 없음: Idle 애니메이션 유지 (매 프레임 댐핑)
        _stateMachine.Animator.SetMoveSpeed(0f);
    }
}