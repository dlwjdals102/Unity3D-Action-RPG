using UnityEngine;

/// <summary>
/// 회피 상태. 이동 방향으로 구르며 무적 프레임이 적용됩니다.
/// - 진입 시 이동 방향으로 돌진
/// - 무적 프레임 구간 (Animation Event로 제어)
/// - 애니메이션 종료 → Idle 복귀
/// </summary>
public class DodgeState : BaseState
{
    private bool _dodgeFinished;
    private Vector3 _dodgeDirection;

    public bool IsInvincible { get; private set; }

    public DodgeState(PlayerStateMachine.PlayerStateContext context) : base(context) { }

    public override void Enter()
    {
        Controller.SetCanMove(false);

        IsInvincible = false;
        _dodgeFinished = false;

        // 이동 입력 방향으로 회피 (입력 없으면 캐릭터 정면)
        Vector2 moveInput = Input.MoveInput;
        if (moveInput.magnitude > 0.1f)
        {
            // 카메라 기준 방향 계산
            Transform cam = Camera.main.transform;
            Vector3 forward = cam.forward;
            Vector3 right = cam.right;
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            _dodgeDirection = (forward * moveInput.y + right * moveInput.x).normalized;
        }
        else
        {
            _dodgeDirection = Ctx.Transform.forward;
        }

        // 회피 방향으로 캐릭터 즉시 회전
        if (_dodgeDirection != Vector3.zero)
            Ctx.Transform.rotation = Quaternion.LookRotation(_dodgeDirection);

        // 애니메이션 재생
        Animator.PlayDodge();

        // 입력 버퍼 초기화
        Input.ClearAllBuffers();

        // 애니메이션 이벤트 구독
        Animator.OnDodgeInvincibleStart += OnInvincibleStart;
        Animator.OnDodgeInvincibleEnd += OnInvincibleEnd;
        Animator.OnAttackEnd += OnDodgeEnd;  // 회피 종료도 같은 이벤트 재활용
    }

    public override void Update()
    {
        if (_dodgeFinished)
        {
            // 이동 입력 있으면 Move, 없으면 Idle
            if (Input.MoveInput.magnitude > 0.1f)
                Owner.TransitionTo(Define.CharacterState.Move);
            else
                Owner.TransitionTo(Define.CharacterState.Idle);
            return;
        }

        // 회피 중 이동 방향으로 돌진
        float dodgeSpeed = Controller.CurrentSpeed > 0 ? 8f : 6f;
        Controller.AddForce(_dodgeDirection * dodgeSpeed * Time.deltaTime);
    }

    public override void Exit()
    {
        IsInvincible = false;
        Controller.SetCanMove(true);

        // 이벤트 구독 해제
        Animator.OnDodgeInvincibleStart -= OnInvincibleStart;
        Animator.OnDodgeInvincibleEnd -= OnInvincibleEnd;
        Animator.OnAttackEnd -= OnDodgeEnd;
    }

    private void OnInvincibleStart()
    {
        IsInvincible = true;
    }

    private void OnInvincibleEnd()
    {
        IsInvincible = false;
    }

    private void OnDodgeEnd()
    {
        _dodgeFinished = true;
    }
}