using UnityEngine;

/// <summary>
/// 피격 상태. 데미지를 받으면 짧은 경직 후 Idle로 복귀합니다.
/// - 진입 시 이동 불가 + 피격 애니메이션
/// - 경직 시간 경과 후 자동 복귀
/// - 경직 중에도 연속 피격 가능 (경직 시간 리셋)
/// </summary>
public class HitState : BaseState
{
    private bool _hitFinished = false;

    public HitState(PlayerStateMachine.PlayerStateContext context) : base(context) { }

    public override void Enter()
    {
        Controller.SetCanMove(false);
        Controller.StopMovement();

        // 피격 애니메이션 재생
        Animator.PlayHit();

        // 입력 버퍼 초기화 (피격 중 쌓인 입력 무시)
        Input.ClearAllBuffers();

        // 애니메이션 종료 이벤트 구독
        Animator.OnHitEnd += OnHitAnimationEnd;
    }

    public override void Update()
    {
        // 애니메이션 이벤트로 종료 신호를 받아야 복귀
        // 안전장치로 최대 1.5초 후 강제 종료
        if (!_hitFinished && Owner.FSM.StateTime < 1.5f)
            return;

        if (Input.MoveInput.magnitude > 0.1f)
            Owner.TransitionTo(Define.CharacterState.Move);
        else
            Owner.TransitionTo(Define.CharacterState.Idle);
    }

    public override void Exit()
    {
        Controller.SetCanMove(true);
        Animator.OnHitEnd -= OnHitAnimationEnd;
    }

    private void OnHitAnimationEnd()
    {
        _hitFinished = true;
    }
}

/// <summary>
/// 사망 상태. 최종 상태로, 다른 상태로 전환되지 않습니다.
/// - 사망 애니메이션 재생
/// - GameManager에 사망 이벤트 전파
/// - 이후 모든 입력 무시
/// </summary>
public class DieState : BaseState
{
    public DieState(PlayerStateMachine.PlayerStateContext context) : base(context) { }

    public override void Enter()
    {
        Controller.SetCanMove(false);
        Controller.StopMovement();

        // 사망 애니메이션 재생
        Animator.PlayDie();

        // 입력 완전 차단
        Input.ClearAllBuffers();

        // GameManager에 사망 알림
        if (GameManager.HasInstance)
            GameManager.Instance.NotifyPlayerDeath();
    }

    public override void Update()
    {
        // 아무것도 하지 않음 — 최종 상태
    }

    public override void Exit()
    {
        // 사망 상태에서 나가는 경우는 리스타트뿐
        Controller.SetCanMove(true);
    }
}