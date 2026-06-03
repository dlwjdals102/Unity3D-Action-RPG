using UnityEngine;

/// <summary>
/// 보스의 기본 근접 공격 (1회). 패턴 풀의 한 패턴으로, "1회 발동 → ChaseState 복귀" 가 핵심.
/// 
/// 일반 MeleeEnemyAttackState 는 공격 후 자기 안에서 쿨다운+재공격(붙으면 계속 때림)하지만,
/// 보스는 모든 패턴이 "1회 발동 → ChaseState 복귀 → 선택기 쿨다운 → 다음 패턴(랜덤)" 흐름이라,
/// 이 State 는 공격 1회만 하고 끝나면 무조건 ChaseState 로 돌아간다.
/// (쿨다운/다음 패턴 선택은 BossChaseState 의 선택기가 관리 - 책임 분리)
/// 
/// 타격은 애니메이션 이벤트 → BossAttacker.PerformHit.
/// </summary>
public class BossAttackState : EnemyStateBase
{
    public BossAttackState(EnemyStateMachineBase stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // 이동 정지 + Target 향함 (1회 즉시 회전, 다크소울 표준)
        _stateMachine.Movement.StopMoving();
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
        }

        // 공격 애니메이션 (IsAttackFinished 리셋)
        _stateMachine.Animator.PlayAttack();
        _stateMachine.Animator.SetMoveSpeed(0f);
    }

    public override void OnUpdate()
    {
        // 공격 종료 시 (Animation Event 가 IsAttackFinished = true) → ChaseState 복귀
        // 쿨다운/다음 패턴은 ChaseState 의 선택기가 결정 (1회 발동 원칙)
        if (_stateMachine.Animator.IsAttackFinished)
        {
            // 패턴 1회 종료 → 쿨다운 대기
            ((BossStateMachine)_stateMachine).ToCooldown();
        }

        // 공격 도중에는 회전/이동 없음 (다크소울 표준, 회피 기회)
    }
}