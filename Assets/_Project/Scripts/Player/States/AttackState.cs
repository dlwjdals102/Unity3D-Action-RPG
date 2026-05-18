using UnityEngine;

/// <summary>
/// 플레이어 공격 상태 (3단 콤보).
/// 단일 상태가 _comboIndex 변수로 1타, 2타, 3타 진행을 모두 관리한다.
/// 콤보 윈도우 안에 좌클릭 입력 시 다음 콤보 예약 (_comboQueued),
/// 공격 종료 시점에 예약된 입력 + 스태미나 충분 시 다음 콤보 진행, 부족 시 종료.
/// 회피 입력 시 즉시 캔슬 (스태미나 충분 시), 점프/이동은 캔슬 불가 (소울라이크 표준).
/// 1타 진입 비용은 IdleState/MoveState/LandState 의 공격 분기에서 미리 체크 + 소모됨.
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
        // (1타 스태미나 소모는 진입 측 (IdleState/MoveState/LandState) 의 분기에서 이미 처리됨)
        StartCombo(_comboIndex);
    }

    public override void OnUpdate()
    {
        // 1. 회피 입력 → DodgeState (캔슬 가능)
        // 스태미나 충분 시에만 회피 가능. TryConsume 으로 가능 체크 + 소모 동시 처리.
        if (_stateMachine.Controller.DodgeRequested &&
            _stateMachine.Movement.CanDodge &&
            _stateMachine.Stamina.TryConsume(_stateMachine.Movement.DodgeStaminaCost))
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
                // 다음 콤보 시도: 스태미나 충분 시 진행, 부족 시 종료
                int nextCombo = _comboIndex + 1;
                float cost = _stateMachine.Attacker.GetComboStaminaCost(nextCombo);

                if (_stateMachine.Stamina.TryConsume(cost))
                {
                    _comboIndex = nextCombo;
                    _comboQueued = false;
                    StartCombo(_comboIndex);
                }
                else
                {
                    // 스태미나 부족 → 콤보 종료
                    ExitToIdleOrMove();
                }
            }
            else
            {
                // 콤보 큐 없음 또는 최대 콤보 도달 → 종료
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