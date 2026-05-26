using UnityEngine;

/// <summary>
/// 원거리 적의 공격 상태 (발사).
/// 근접 MeleeEnemyAttackState 와 다름:
/// - 즉시 회전(다크소울) 대신 LookAt 조준 (멀리서 천천히 조준 → 회피 여지).
/// - 연속 공격 대신 발사 쿨다운 (Config.AttackCooldown).
/// 
/// 흐름:
/// - OnEnter: 정지 + 발사 애니메이션 + 쿨다운 타이머 시작.
///   발사체 생성은 Animation Event (OnAttackHit → RangedAttacker.PerformHit) 가 담당.
/// - OnUpdate: Target 조준 (LookAt) + 발사 애니메이션 종료 후 거리/쿨다운 분기.
///   - 발사 거리 밖: ToChase (다시 접근)
///   - 발사 거리 안 + 쿨다운 끝: 재발사
///   - 발사 거리 안 + 쿨다운 중: 조준하며 대기
/// 
/// _stateMachine 은 베이스 타입. 전환은 ToChase 의도 (원거리 파생이 EnemyChaseState 로).
/// </summary>
public class RangedEnemyAttackState : EnemyStateBase
{
    private float _cooldownTimer;

    public RangedEnemyAttackState(EnemyStateMachineBase stateMachine)
        : base(stateMachine)
    {
    }

    public override void OnEnter()
    {
        // 정지
        _stateMachine.Movement.StopMoving();
        _stateMachine.Animator.SetMoveSpeed(0f);

        // 첫 발사 시작
        FireOnce();
    }

    public override void OnUpdate()
    {
        if (_stateMachine.Target == null) return;

        // 조준 (대기 중에도 Target 추적, 부드러운 회전 → 회피 여지)
        _stateMachine.Movement.LookAt(_stateMachine.Target.position);

        // 쿨다운 감소
        _cooldownTimer -= Time.deltaTime;

        // 발사 애니메이션이 끝났을 때만 다음 행동 결정
        if (!_stateMachine.Animator.IsAttackFinished) return;

        // 거리 체크
        float distance = _stateMachine.Movement.DistanceTo(_stateMachine.Target.position);

        // 발사 거리 밖: 다시 접근 (추격)
        if (distance > _stateMachine.AttackRange)
        {
            _stateMachine.ToChase();
            return;
        }

        // 발사 거리 안 + 쿨다운 끝: 재발사
        if (_cooldownTimer <= 0f)
        {
            FireOnce();
        }
        else
        {
            // 쿨다운 중: Idle 대기 (발사 애니메이션 마지막 프레임 고정 방지)
            // 매 프레임 호출로 댐핑 수렴. LookAt 조준은 위에서 계속.
            _stateMachine.Animator.PlayIdle();
        }

        // 쿨다운 중이면 조준하며 대기 (위 LookAt 계속)
    }

    /// <summary>
    /// 발사 1회 시작. 발사 애니메이션 재생 + 쿨다운 타이머 리셋.
    /// 실제 발사체 생성은 Animation Event (OnAttackHit) 가 RangedAttacker.PerformHit 호출.
    /// </summary>
    private void FireOnce()
    {
        // Target 향함 (발사 순간 1회 즉시 회전 → 정확한 발사 방향)
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
        }

        // 발사 애니메이션 (IsAttackFinished 리셋)
        _stateMachine.Animator.PlayAttack();

        // 쿨다운 시작
        _cooldownTimer = _stateMachine.AttackCooldown;
    }
}