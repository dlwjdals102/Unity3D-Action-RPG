using UnityEngine;

/// <summary>
/// 공격 상태. 콤보 시스템과 연동됩니다.
/// - 진입 시 이동 불가
/// - 콤보 윈도우 중 좌클릭 → 다음 콤보
/// - 애니메이션 종료 → Idle/Move 복귀
/// 
/// [콤보 흐름]
/// 좌클릭 → Attack1 재생 → 콤보 윈도우 열림 → 좌클릭 → Attack2 → ... → Attack3 → 종료
/// </summary>
public class AttackState : BaseState
{
    private int _comboIndex;
    private bool _comboWindowOpen;
    private bool _comboRequested;
    private bool _attackFinished;

    public AttackState(PlayerStateMachine.PlayerStateContext context) : base(context) { }

    public override void Enter()
    {
        // 이동 불가
        Controller.SetCanMove(false);
        Controller.StopMovement();

        // 콤보 초기화
        _comboIndex = 0;
        _comboWindowOpen = false;
        _comboRequested = false;
        _attackFinished = false;

        // 첫 공격 애니메이션 재생
        Animator.PlayAttack(_comboIndex);

        // 입력 버퍼 소비 (중복 방지)
        Input.ClearAllBuffers();

        // 애니메이션 이벤트 구독
        Animator.OnComboWindowOpen += OnComboWindowOpen;
        Animator.OnAttackEnd += OnAttackEnd;
    }

    public override void Update()
    {
        // 공격 애니메이션이 끝났으면 복귀
        if (_attackFinished)
        {
            ReturnToLocomotion();
            return;
        }

        // 콤보 윈도우가 열려있을 때 공격 입력 체크
        if (_comboWindowOpen && Input.ConsumeAttack())
        {
            _comboRequested = true;
        }

        // 회피 입력으로 공격 캔슬 (캔슬 가능 구간에서만)
        if (_comboWindowOpen && Input.ConsumeDodge())
        {
            if (Input.MoveInput.magnitude > 0.1f)
            {
                Owner.TransitionTo(Define.CharacterState.Dodge);
            }
        }
    }

    public override void Exit()
    {
        Controller.SetCanMove(true);

        // 이벤트 구독 해제
        Animator.OnComboWindowOpen -= OnComboWindowOpen;
        Animator.OnAttackEnd -= OnAttackEnd;
    }

    // ── 애니메이션 이벤트 핸들러 ──

    private void OnComboWindowOpen()
    {
        _comboWindowOpen = true;

        // 이미 입력이 버퍼에 있었으면 즉시 다음 콤보
        if (_comboRequested)
        {
            AdvanceCombo();
        }
    }

    private void OnAttackEnd()
    {
        // 콤보 입력이 있으면 다음 콤보 진행
        if (_comboRequested && !_attackFinished)
        {
            AdvanceCombo();
        }
        else
        {
            _attackFinished = true;
        }
    }

    private void AdvanceCombo()
    {
        _comboIndex++;

        if (_comboIndex >= Define.Balance.MaxComboCount)
        {
            // 최대 콤보 도달 → 종료
            _attackFinished = true;
            return;
        }

        // 다음 콤보 애니메이션 재생
        _comboWindowOpen = false;
        _comboRequested = false;
        Animator.PlayAttack(_comboIndex);
    }

    private void ReturnToLocomotion()
    {
        if (Input.MoveInput.magnitude > 0.1f)
            Owner.TransitionTo(Define.CharacterState.Move);
        else
            Owner.TransitionTo(Define.CharacterState.Idle);
    }
}