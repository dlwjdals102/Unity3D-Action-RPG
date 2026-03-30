using UnityEngine;

/// <summary>
/// 대기 상태. 입력 없이 가만히 서있는 상태입니다.
/// → Move: 이동 입력 감지
/// → Attack: 좌클릭
/// → Dodge: Space (이동 입력 + Space)
/// </summary>
public class IdleState : BaseState
{
    public IdleState(PlayerStateMachine.PlayerStateContext context) : base(context) { }

    public override void Enter()
    {
        Controller.SetCanMove(true);
        Controller.StopMovement();
    }

    public override void Update()
    {
        // 전투 입력 체크 (공격, 회피)
        if (CheckCombatTransitions()) return;

        // 이동 입력 → Move 전환
        if (Input.MoveInput.magnitude > 0.1f)
        {
            Owner.TransitionTo(Define.CharacterState.Move);
        }
    }
}

/// <summary>
/// 이동 상태. WASD로 이동 중인 상태입니다.
/// → Idle: 이동 입력 없음
/// → Attack: 좌클릭
/// → Dodge: Space
/// </summary>
public class MoveState : BaseState
{
    public MoveState(PlayerStateMachine.PlayerStateContext context) : base(context) { }

    public override void Enter()
    {
        Controller.SetCanMove(true);
    }

    public override void Update()
    {
        // 전투 입력 체크
        if (CheckCombatTransitions()) return;

        // 입력 없음 → Idle 전환
        if (Input.MoveInput.magnitude < 0.1f)
        {
            Owner.TransitionTo(Define.CharacterState.Idle);
        }
    }
}