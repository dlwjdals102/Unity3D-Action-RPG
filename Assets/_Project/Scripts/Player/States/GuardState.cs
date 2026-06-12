using UnityEngine;

/// <summary>
/// 플레이어 가드 상태 (우클릭 유지, 방패 착용 시에만 진입 가능).
/// - 진입 직후 짧은 시간은 "패리 윈도우" (저스트 가드 - 이때 피격되면 패리 성공)
/// - 가드 중 피격 데미지 감소/스태미나 소모는 PlayerHealth 가 처리 ([G-2])
/// - 제자리 가드 (이동 불가), 회피로 캔슬 가능
/// </summary>
public class GuardState : PlayerStateBase
{
    // 패리 윈도우: 가드 진입 직후 이 시간 안에 피격되면 패리 (저스트 가드)
    private const float ParryWindowDuration = 0.18f;

    private float _parryWindowEndTime;

    public GuardState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    /// <summary>지금이 패리 윈도우인가 (가드 진입 직후). 피격 시 PlayerHealth 가 조회.</summary>
    public bool IsParryWindow => Time.time < _parryWindowEndTime;

    public override void OnEnter()
    {
        _parryWindowEndTime = Time.time + ParryWindowDuration;

        // 제자리 가드: 이동 애니 정지
        _stateMachine.Animator.SetMoveSpeed(0f);

        // 가드 자세 진입
        _stateMachine.Animator.SetGuarding(true);
    }

    public override void OnUpdate()
    {
        // 1. 회피 입력 → 가드 캔슬 (소울라이크: 가드 중 구르기 가능)
        if (_stateMachine.Controller.DodgeRequested &&
            _stateMachine.Movement.CanDodge &&
            _stateMachine.Stamina.TryConsume(_stateMachine.Movement.DodgeStaminaCost))
        {
            _stateMachine.ChangeState(_stateMachine.DodgeState);
            return;
        }

        // 2. 가드 입력 해제 → Idle 복귀
        if (!_stateMachine.Controller.IsGuardHeld)
        {
            _stateMachine.ChangeState(_stateMachine.IdleState);
            return;
        }

        // 3. 가드 유지 조건 상실 (방패 해제 등) → Idle
        if (!_stateMachine.CanGuard)
        {
            _stateMachine.ChangeState(_stateMachine.IdleState);
            return;
        }

        // 4. 공중 → Fall
        if (!_stateMachine.Movement.IsGrounded)
        {
            _stateMachine.ChangeState(_stateMachine.FallState);
            return;
        }
    }

    public override void OnExit()
    {
        // 가드 자세 해제 (어떤 경로로 나가든 - Idle/회피/낙하)
        _stateMachine.Animator.SetGuarding(false);
    }
}