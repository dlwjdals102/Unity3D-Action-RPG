using UnityEngine;

/// <summary>
/// 스킬 사용 상태. SkillExecutor와 연동하여 스킬을 실행합니다.
/// - 진입 시 이동 불가
/// - 스킬 애니메이션 재생
/// - Animation Event로 데미지 판정
/// - 애니메이션 종료 시 Idle/Move 복귀
/// </summary>
public class SkillState : BaseState
{
    private SkillExecutor _skillExecutor;
    private int _currentSlotIndex;
    private bool _skillFinished;

    public SkillState(PlayerStateMachine.PlayerStateContext context) : base(context)
    {
        _skillExecutor = context.Controller.GetComponent<SkillExecutor>();
    }

    /// <summary>사용할 스킬 슬롯을 설정합니다. Enter() 전에 호출합니다.</summary>
    public void SetSkillSlot(int slotIndex)
    {
        _currentSlotIndex = slotIndex;
    }

    public override void Enter()
    {
        Controller.SetCanMove(false);
        Controller.StopMovement();

        _skillFinished = false;

        // 무기 HitBox 억제 (스킬은 범위 판정 사용)
        var hitBoxController = Controller.GetComponent<HitBoxController>();
        hitBoxController?.SetSuppressed(true);

        // 스킬 실행
        if (_skillExecutor != null)
        {
            bool success = _skillExecutor.ExecuteSkill(_currentSlotIndex);
            if (!success)
            {
                // 쿨다운 중이거나 스킬 없음 → 즉시 복귀
                _skillFinished = true;
                return;
            }
        }

        Input.ClearAllBuffers();

        // 애니메이션 종료 이벤트 구독
        Animator.OnAttackEnd += OnSkillEnd;

        // 히트 프레임에서 스킬 데미지 적용
        Animator.OnAttackHitFrame += OnSkillHitFrame;
    }

    public override void Update()
    {
        if (_skillFinished)
        {
            if (Input.MoveInput.magnitude > 0.1f)
                Owner.TransitionTo(Define.CharacterState.Move);
            else
                Owner.TransitionTo(Define.CharacterState.Idle);
        }
    }

    public override void Exit()
    {
        Controller.SetCanMove(true);

        // 무기 HitBox 억제 해제
        var hitBoxController = Controller.GetComponent<HitBoxController>();
        hitBoxController?.SetSuppressed(false);

        Animator.OnAttackEnd -= OnSkillEnd;
        Animator.OnAttackHitFrame -= OnSkillHitFrame;
    }

    private void OnSkillHitFrame()
    {
        _skillExecutor?.ApplySkillDamage(_currentSlotIndex);
    }

    private void OnSkillEnd()
    {
        _skillFinished = true;
    }
}