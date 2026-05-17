using UnityEngine;

/// <summary>
/// 플레이어 공격 상태 (3단 콤보).
/// 단일 상태가 _comboIndex 변수로 1타, 2타, 3타 진행을 모두 관리한다.
/// 콤보 윈도우 안에 좌클릭 입력 시 다음 콤보 예약 (_comboQueued),
/// 공격 종료 시점에 예약된 입력에 따라 다음 콤보 또는 종료 결정.
/// 콤보 진행마다 PlayerAttacker 에 데미지를 설정하여 OnAttackHit Event 가 정확한 데미지로 적용한다.
/// 회피 입력 시 즉시 캔슬, 점프/이동은 캔슬 불가 (소울라이크 표준).
/// </summary>
public class AttackState : PlayerStateBase
{
    private const float MoveInputThreshold = 0.01f;
    private const int MaxComboIndex = 2;  // 0, 1, 2 (1타, 2타, 3타)

    private int _comboIndex;
    private bool _comboQueued;

    public AttackState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // 콤보 초기화 (인스턴스 재사용 안전)
        _comboIndex = 0;
        _comboQueued = false;

        // 1타 데미지 설정 후 애니메이션 재생
        StartCombo(_comboIndex);
    }

    public override void OnUpdate()
    {
        // 1. 회피 입력 → DodgeState (캔슬 가능)
        if (_stateMachine.Controller.DodgeRequested && _stateMachine.Movement.CanDodge)
        {
            _stateMachine.ChangeState(_stateMachine.DodgeState);
            return;
        }

        // 2. 콤보 윈도우 안에 좌클릭 → 다음 콤보 예약
        if (_stateMachine.Animator.IsComboWindowOpen && _stateMachine.Controller.AttackRequested)
        {
            _comboQueued = true;
        }

        // 3. 공격 애니메이션 종료 시점 처리
        if (_stateMachine.Animator.IsAttackFinished)
        {
            if (_comboQueued && _comboIndex < MaxComboIndex)
            {
                // 다음 콤보 진행
                _comboIndex++;
                _comboQueued = false;
                StartCombo(_comboIndex);
            }
            else
            {
                // 콤보 종료 → 입력 기준 분기
                ExitToIdleOrMove();
            }
        }
    }

    /// <summary>
    /// 콤보 시작 처리. 데미지 설정 후 애니메이션 재생.
    /// SetCurrentCombo 가 먼저 호출되어야 PlayAttack 의 Animation Event 가
    /// 정확한 데미지로 적용된다.
    /// </summary>
    private void StartCombo(int comboIndex)
    {
        _stateMachine.Attacker.SetCurrentCombo(comboIndex);
        _stateMachine.Animator.PlayAttack(comboIndex);
    }

    /// <summary>
    /// 콤보 종료 또는 캔슬 시 다음 상태 결정.
    /// 이동 입력 있으면 MoveState, 없으면 IdleState.
    /// </summary>
    private void ExitToIdleOrMove()
    {
        Vector2 input = _stateMachine.Controller.MoveInput;
        if (input.sqrMagnitude > MoveInputThreshold)
            _stateMachine.ChangeState(_stateMachine.MoveState);
        else
            _stateMachine.ChangeState(_stateMachine.IdleState);
    }
}