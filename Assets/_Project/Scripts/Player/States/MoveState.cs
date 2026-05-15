using UnityEngine;

/// <summary>
/// 플레이어 이동 상태 (걷기/뛰기).
/// 카메라 기준 방향으로 이동하며 입력에 따라 DodgeState, JumpState, IdleState 로,
/// 공중 감지 시 FallState 로 자동 전환된다.
/// 진입 시 Animator 를 Locomotion 상태로 명시적 전환한다.
/// </summary>
public class MoveState : PlayerStateBase
{
    private const float MoveInputThreshold = 0.01f;

    public MoveState(PlayerStateMachine stateMachine) : base(stateMachine) { }

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

        // 3. 점프 입력 → JumpState
        if (_stateMachine.Controller.JumpRequested)
        {
            _stateMachine.ChangeState(_stateMachine.JumpState);
            return;
        }

        // 4. 입력 없음 → IdleState
        Vector2 input = _stateMachine.Controller.MoveInput;
        if (input.sqrMagnitude < MoveInputThreshold)
        {
            _stateMachine.ChangeState(_stateMachine.IdleState);
            return;
        }

        // 5. 이동 처리 (기본 행동)
        Vector3 moveDirection = _stateMachine.Movement.GetCameraRelativeDirection(input);

        bool isSprinting = _stateMachine.Controller.IsSprintHeld;
        float speed = isSprinting
            ? _stateMachine.Movement.RunSpeed
            : _stateMachine.Movement.WalkSpeed;

        _stateMachine.Movement.RequestMovement(moveDirection * speed);
        _stateMachine.Movement.ApplyRotation(moveDirection);

        float normalizedSpeed = isSprinting ? 1f : 0.5f;
        _stateMachine.Animator.SetMoveSpeed(normalizedSpeed);

        // 다음 단계에서 추가될 전환:
        // - 좌클릭 → AttackState (Week 3 후반)
    }
}