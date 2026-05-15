using UnityEngine;

/// <summary>
/// 플레이어 회피 상태.
/// 회피 시작 시 방향 결정 + 즉시 회전, 일정 속도로 이동하며
/// Animation Event 기반으로 종료 시점을 판정한다.
/// 종료 시 입력에 따라 IdleState 또는 MoveState 로 전환한다.
/// 회피 중에는 점프/이동 등 다른 입력을 받지 않는다 (무적 시간 보호 의도).
/// </summary>
public class DodgeState : PlayerStateBase
{
    private const float MoveInputThreshold = 0.01f;

    private Vector3 _dodgeDirection;

    public DodgeState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // 회피 방향 결정
        Vector2 moveInput = _stateMachine.Controller.MoveInput;
        if (moveInput.sqrMagnitude > MoveInputThreshold)
        {
            // 입력 방향으로 회피
            _dodgeDirection = _stateMachine.Movement.GetCameraRelativeDirection(moveInput);
            _stateMachine.Movement.SetRotationImmediate(_dodgeDirection);
        }
        else
        {
            // 입력 없으면 캐릭터 정면으로
            _dodgeDirection = _stateMachine.transform.forward;
        }

        // 회피 애니메이션 재생 (IsDodgeFinished 플래그 자동 리셋)
        _stateMachine.Animator.PlayDodge();
    }

    public override void OnUpdate()
    {
        // 회피 종료 (Animation Event 기반, 확실한 시그널)
        if (_stateMachine.Animator.IsDodgeFinished)
        {
            // 입력에 따라 다음 상태 결정
            Vector2 input = _stateMachine.Controller.MoveInput;
            if (input.sqrMagnitude > MoveInputThreshold)
                _stateMachine.ChangeState(_stateMachine.MoveState);
            else
                _stateMachine.ChangeState(_stateMachine.IdleState);
            return;
        }

        // 회피 이동 (일정 속도, Animation Event 가 종료 알릴 때까지)
        _stateMachine.Movement.RequestMovement(_dodgeDirection * _stateMachine.Movement.DodgeSpeed);
    }

    public override void OnExit()
    {
        // 회피 종료 시 쿨다운 시작 (다음 회피까지 대기 시간)
        _stateMachine.Movement.StartDodgeCooldown();
    }
}