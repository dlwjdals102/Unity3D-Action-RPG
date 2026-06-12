using UnityEngine;

/// <summary>
/// 플레이어 이동 상태 (걷기/뛰기).
/// 카메라 기준 방향으로 이동하며 입력에 따라 DodgeState, AttackState, JumpState, IdleState 로,
/// 공중 감지 시 FallState 로 자동 전환된다.
/// 진입 시 Animator 를 Locomotion 상태로 명시적 전환한다.
/// 달리기는 두 단계 임계값으로 관리: 새로 시작에 MinStaminaToStartSprint 이상 필요,
/// 이미 달리는 중이면 0 초과까지 유지 (회복-소모 무한 사이클 회피).
/// 회피/공격 입력은 스태미나 충분 시에만 처리.
/// </summary>
public class MoveState : PlayerStateBase
{
    private const float MoveInputThreshold = 0.01f;
    private const float MinStaminaToKeepSprint = 0.01f;

    // 두 단계 임계값을 위한 인스턴스 상태
    private bool _wasSprintingLastFrame;

    public MoveState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // Animator 를 Locomotion 으로 명시적 전환 (Single Source of Truth)
        _stateMachine.Animator.PlayLocomotion();
    }

    public override void OnExit()
    {
        // 다음 MoveState 진입 시 "새로 시작" 으로 판단되도록 리셋
        _wasSprintingLastFrame = false;
    }

    public override void OnUpdate()
    {
        // 1. 공중 감지 → FallState (최우선, 절벽에서 떨어짐 케이스)
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

        // 2.5 가드 입력(유지) + 방패 착용 → GuardState
        if (_stateMachine.Controller.IsGuardHeld && _stateMachine.CanGuard)
        {
            _stateMachine.ChangeState(_stateMachine.GuardState);
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

        // 4. 점프 입력 → JumpState
        if (_stateMachine.Controller.JumpRequested)
        {
            _stateMachine.ChangeState(_stateMachine.JumpState);
            return;
        }

        // 5. 입력 없음 → IdleState
        Vector2 input = _stateMachine.Controller.MoveInput;
        if (input.sqrMagnitude < MoveInputThreshold)
        {
            _stateMachine.ChangeState(_stateMachine.IdleState);
            return;
        }

        // 6. 이동 처리 (기본 행동)
        Vector3 moveDirection = _stateMachine.Movement.GetCameraRelativeDirection(input);

        bool wantsSprint = _stateMachine.Controller.IsSprintHeld;
        bool isSprinting = CanSprint(wantsSprint);

        // 달리기 중이면 스태미나 매 프레임 소모
        if (isSprinting)
        {
            _stateMachine.Stamina.ConsumeContinuous(_stateMachine.Movement.SprintStaminaCostPerSecond);
        }

        // 다음 프레임을 위한 상태 갱신
        _wasSprintingLastFrame = isSprinting;

        float speed = isSprinting
            ? _stateMachine.Movement.RunSpeed
            : _stateMachine.Movement.WalkSpeed;

        _stateMachine.Movement.RequestMovement(moveDirection * speed);

        // 락온 중이면 적을 바라보며 이동(스트레이프), 아니면 이동 방향으로 회전
        Vector3 lookDirection = GetLookDirection(moveDirection);
        _stateMachine.Movement.ApplyRotation(lookDirection);

        float normalizedSpeed = isSprinting ? 1f : 0.5f;
        _stateMachine.Animator.SetMoveSpeed(normalizedSpeed);
    }

    /// <summary>
    /// 회전이 바라볼 방향을 결정한다.
    /// 락온 중: 타겟(적) 방향 → 적을 바라본 채 이동(스트레이프).
    /// 평소: 이동 방향 → 가는 쪽으로 몸을 돌림.
    /// </summary>
    private Vector3 GetLookDirection(Vector3 moveDirection)
    {
        LockOnSystem lockOn = _stateMachine.LockOn;
        if (lockOn != null && lockOn.IsLockedOn)
        {
            Vector3 toTarget = lockOn.CurrentTarget.position - _stateMachine.transform.position;
            toTarget.y = 0f;  // 수평 방향만 (위아래 기울임 방지)
            if (toTarget.sqrMagnitude > 0.0001f)
            {
                return toTarget;
            }
        }
        return moveDirection;  // 락온 아님 또는 타겟과 겹침 → 이동 방향
    }

    /// <summary>
    /// 달리기 가능 여부 판단. 두 단계 임계값으로 회복-소모 무한 사이클 회피.
    /// 새로 시작: MinStaminaToStartSprint 이상 필요 (충분한 회복 후 재시작)
    /// 이미 달리는 중: 0 초과면 유지 (끝까지 활용)
    /// </summary>
    private bool CanSprint(bool wantsSprint)
    {
        if (!wantsSprint) return false;

        if (_wasSprintingLastFrame)
        {
            // 이미 달리는 중: 0 초과까지 끝까지 활용
            return _stateMachine.Stamina.HasEnough(MinStaminaToKeepSprint);
        }
        else
        {
            // 새로 시작: 임계값 이상이어야 시작 가능
            return _stateMachine.Stamina.HasEnough(_stateMachine.Movement.MinStaminaToStartSprint);
        }
    }
}