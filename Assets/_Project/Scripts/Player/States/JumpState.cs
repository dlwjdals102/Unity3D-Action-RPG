using UnityEngine;

/// <summary>
/// 플레이어 점프 상승 상태.
/// 점프 시작 시 수직 속도를 설정하고, Y 속도가 음수가 되면 FallState 로 전환한다.
/// 공중에서 Air Control(약화된 수평 이동) 가능.
/// </summary>
public class JumpState : PlayerStateBase
{
    private const float MoveInputThreshold = 0.01f;
    private const float AirControlMultiplier = 0.7f;  // 공중 이동은 지상 속도의 70%

    public JumpState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        Debug.Log("[JumpState] OnEnter");
        // 점프 발동: 수직 속도 설정 + 애니메이션 재생
        _stateMachine.Movement.Jump();
        _stateMachine.Animator.PlayJump();
    }

    public override void OnUpdate()
    {
        // 공중 이동 (Air Control)
        ApplyAirMovement();

        // 낙하 시작 시 FallState 로 전환
        if (_stateMachine.Movement.VerticalVelocity.y < 0f)
        {
            _stateMachine.ChangeState(_stateMachine.FallState);
            return;
        }
    }

    private void ApplyAirMovement()
    {
        Vector2 input = _stateMachine.Controller.MoveInput;
        if (input.sqrMagnitude < MoveInputThreshold)
            return;

        // 카메라 기준 방향 + 약화된 속도
        Vector3 moveDir = _stateMachine.Movement.GetCameraRelativeDirection(input);
        bool isSprinting = _stateMachine.Controller.IsSprintHeld;
        float baseSpeed = isSprinting
            ? _stateMachine.Movement.RunSpeed
            : _stateMachine.Movement.WalkSpeed;
        float airSpeed = baseSpeed * AirControlMultiplier;

        _stateMachine.Movement.RequestMovement(moveDir * airSpeed);

        // 공중에서도 회전 처리 (이동 방향을 바라봄)
        _stateMachine.Movement.ApplyRotation(moveDir);
    }
}