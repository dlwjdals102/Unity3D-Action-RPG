using UnityEngine;

/// <summary>
/// 적의 근접 공격 상태.
/// OnEnter 시점에 Target 향함 (1회 즉시 회전) + 이동 정지 + 공격 애니메이션 재생.
/// 공격 모션 도중에는 회전 안 함 (다크소울 표준, 플레이어 회피 기회).
/// 
/// 공격 종료 후 쿨다운 (AttackCooldown, 모든 적 공통):
/// - 쿨다운 중: Idle + Target 조준 (LookAt) 대기. 멀어지면 추격.
/// - 쿨다운 끝 + 가까움: 재공격.
/// AttackCooldown 0 이면 쿨다운 없이 연속 공격 (기존 좀비 동작).
/// 
/// (엘리트 EliteComboAttackState 의 쿨다운 처리와 거의 동일 - 미래 공통 추출 후보)
/// </summary>
public class MeleeEnemyAttackState : EnemyStateBase
{
    private bool _isCoolingDown;
   /* private float _cooldownTimer;*/

    public MeleeEnemyAttackState(EnemyStateMachineBase stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // 1. 이동 정지
        _stateMachine.Movement.StopMoving();

        // 2. 쿨다운 준비됐으면 즉시 스윙, 아니면 쿨다운 대기로 시작
        //    (전투 진입 시 쿨다운이 걸려 있으면 첫 스윙도 한 박자 늦음)
        if (_stateMachine.IsAttackReady)
        {
            BeginSwing();
        }
        else
        {
            _isCoolingDown = true;
            _stateMachine.Animator.SetMoveSpeed(0f);
        }
    }

    public override void OnUpdate()
    {
        // Target null 안전망
        if (_stateMachine.Target == null) return;

        // === 쿨다운 모드 (공격 종료 후 대기) ===
        if (_isCoolingDown)
        {
            UpdateCooldown();
            return;
        }

        // === 공격 모드 ===
        // 공격 종료 감지 (Animation Event 가 IsAttackFinished = true 설정)
        if (_stateMachine.Animator.IsAttackFinished)
        {
            // 공격 1회 끝 → 쿨다운 진입 (다음 공격 가능 시각 갱신)
            _stateMachine.StartAttackCooldown();
            _isCoolingDown = true;

            // Idle 자세 전환은 UpdateCooldown 이 매 프레임 PlayIdle 로 처리
            // (SetMoveSpeed 댐핑 때문에 1회 호출로는 Idle 도달 못 함)
        }

        // 공격 도중에는 회전/이동 처리 없음 (다크소울 표준)
    }

    /// <summary>
    /// 쿨다운 중 매 프레임 처리.
    /// - Target 조준 (LookAt, 부드러운 회전)
    /// - 멀어지면 추격 (쿨다운 무시)
    /// - 쿨다운 끝 + 가까우면 재공격
    /// AttackCooldown 0 이면 즉시 재공격 (기존 연속 동작).
    /// </summary>
    private void UpdateCooldown()
    {
        // 대기 중 Idle 자세 + Target 조준
        _stateMachine.Animator.PlayIdle();
        _stateMachine.Movement.LookAt(_stateMachine.Target.position);

        float distance = _stateMachine.Movement.DistanceTo(_stateMachine.Target.position);

        // 멀어지면 추격 복귀
        if (distance > _stateMachine.AttackRange)
        {
            _stateMachine.ToChase();
            return;
        }

        // 쿨다운 끝 + 가까움 → 재공격
        if (_stateMachine.IsAttackReady)
        {
            BeginSwing();
        }
    }

    /// <summary>공격 스윙 시작: Target 향해 1회 즉시 회전 + 공격 애니메이션 + 공격 모드.</summary>
    private void BeginSwing()
    {
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
        }
        _stateMachine.Animator.PlayAttack();   // IsAttackFinished 리셋
        _stateMachine.Animator.SetMoveSpeed(0f);
        _isCoolingDown = false;
    }
}