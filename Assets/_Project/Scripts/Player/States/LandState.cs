using UnityEngine;

/// <summary>
/// 플레이어 착지 상태.
/// Land 애니메이션을 트리거하고, 입력 (회피/공격/점프/이동) 또는 Animation Event 기반으로
/// 다른 상태로 전환한다. 확실한 시그널만 신뢰하며 임의의 타이머는 사용하지 않는다.
/// 회피/공격 입력은 스태미나 충분 시에만 처리.
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
        // 스태미나 충분 시에만 회피 가능. TryConsume 으로 가능 체크 + 소모 동시 처리.
        if (_stateMachine.Controller.DodgeRequested &&
            _stateMachine.Movement.CanDodge &&
            _stateMachine.Stamina.TryConsume(_stateMachine.Movement.DodgeStaminaCost))
        {
            _stateMachine.ChangeState(_stateMachine.DodgeState);
            return;
        }

        // 3. 공격 입력 → AttackState (전투 게임 우선)
        // 1타 비용을 진입 시 소모. AttackState 의 OnEnter 는 별도 소모 안 함.
        if (_stateMachine.Controller.AttackRequested &&
            _stateMachine.Stamina.TryConsume(_stateMachine.Attacker.GetComboStaminaCost(0)))
        {
            _stateMachine.ChangeState(_stateMachine.AttackState);
            return;
        }

        // 4. 점프 입력 → JumpState (점프 캔슬)
        if (_stateMachine.Controller.JumpRequested)
        {
            _stateMachine.ChangeState(_stateMachine.JumpState);
            return;
        }

        // 5. 이동 입력 → MoveState (이동 캔슬)
        Vector2 input = _stateMachine.Controller.MoveInput;
        if (input.sqrMagnitude > MoveInputThreshold)
        {
            _stateMachine.ChangeState(_stateMachine.MoveState);
            return;
        }

        // 6. Animation Event 기반 종료 → IdleState
        if (_stateMachine.Animator.IsLandFinished)
        {
            _stateMachine.ChangeState(_stateMachine.IdleState);
        }
    }
}