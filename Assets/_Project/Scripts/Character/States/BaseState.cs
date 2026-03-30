using UnityEngine;

/// <summary>
/// 모든 Player 상태의 베이스 클래스.
/// Context를 통해 Controller, Animator, Input에 접근합니다.
/// </summary>
public abstract class BaseState : IState
{
    protected readonly PlayerStateMachine.PlayerStateContext Ctx;

    // 자주 사용하는 참조 단축
    protected PlayerController Controller => Ctx.Controller;
    protected PlayerAnimator Animator => Ctx.Animator;
    protected PlayerInputHandler Input => Ctx.Input;
    protected PlayerStateMachine Owner => Ctx.Owner;

    protected BaseState(PlayerStateMachine.PlayerStateContext context)
    {
        Ctx = context;
    }

    public virtual void Enter() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }
    public virtual void Exit() { }

    /// <summary>Idle/Move 공통: 공격/회피 입력 체크</summary>
    protected bool CheckCombatTransitions()
    {
        // 공격 입력 확인
        if (Input.ConsumeAttack())
        {
            Owner.TransitionTo(Define.CharacterState.Attack);
            return true;
        }

        // 회피 입력 확인 (이동 입력이 있을 때만 — 다크소울 스타일)
        if (Input.ConsumeDodge())
        {
            if (Input.MoveInput.magnitude > 0.1f)
            {
                Owner.TransitionTo(Define.CharacterState.Dodge);
            }
            else
            {
                // 정지 중 Space = 점프 (PlayerController에서 이미 처리)
            }
            return true;
        }

        return false;
    }
}