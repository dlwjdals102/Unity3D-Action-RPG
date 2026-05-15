using UnityEngine;

/// <summary>
/// 플레이어 낙하 상태.
/// 공중에서 떨어지는 동안 Air Control 을 적용하며,
/// 지면이 감지되면 LandState 로 전환한다.
/// </summary>
public class FallState : PlayerStateBase
{
    private const float MoveInputThreshold = 0.01f;
    private const float AirControlMultiplier = 0.7f;  // 공중 이동은 지상 속도의 70%

    public FallState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        Debug.Log("[FallState] OnEnter");
        // Fall 애니메이션 재생 (코드가 명시적 트리거)
        _stateMachine.Animator.PlayFall();
    }

    public override void OnUpdate()
    {
        // 공중 이동 (Air Control)
        ApplyAirMovement();

        // 지면 감지 시 LandState 로 전환
        if (_stateMachine.Movement.IsGrounded)
        {
            _stateMachine.ChangeState(_stateMachine.LandState);
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
        _stateMachine.Movement.ApplyRotation(moveDir);
    }
}