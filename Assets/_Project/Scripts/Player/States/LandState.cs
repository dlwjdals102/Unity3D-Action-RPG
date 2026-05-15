using UnityEngine;

/// <summary>
/// 플레이어 착지 상태.
/// Land 애니메이션을 트리거하고, 입력(회피/점프/이동) 또는 Animation Event 기반으로
/// 다른 상태로 전환한다. 확실한 시그널만 신뢰하며 임의의 타이머는 사용하지 않는다.
/// </summary>
public class LandState : PlayerStateBase
{
    private const float MoveInputThreshold = 0.01f;

    public LandState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // Land 애니메이션 재생 (IsLandFinished 플래그 자동 리셋)
        _stateMachine.Animator.PlayLand();
    }

    public override void OnUpdate()
    {
        // 1. 공중 감지 → FallState (안전성, 절벽 끝에 착지 후 다시 떨어지는 케이스)
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

        // 3. 점프 입력 → JumpState (점프 캔슬)
        if (_stateMachine.Controller.JumpRequested)
        {
            _stateMachine.ChangeState(_stateMachine.JumpState);
            return;
        }

        // 4. 이동 입력 → MoveState (이동 캔슬)
        Vector2 input = _stateMachine.Controller.MoveInput;
        if (input.sqrMagnitude > MoveInputThreshold)
        {
            _stateMachine.ChangeState(_stateMachine.MoveState);
            return;
        }

        // 5. Animation Event 기반 종료 → IdleState
        if (_stateMachine.Animator.IsLandFinished)
        {
            _stateMachine.ChangeState(_stateMachine.IdleState);
        }

        // 다음 단계에서 추가될 전환:
        // - 좌클릭 → AttackState (Week 3 후반)
    }
}