using UnityEngine;

/// <summary>
/// 적의 공격 상태.
/// OnEnter 시점에 Target 향함 (1회 즉시 회전) + 이동 정지 + 공격 애니메이션 재생.
/// 공격 모션 도중에는 회전 안 함 (다크소울 표준, 플레이어 회피 기회).
/// 공격 종료 시 거리 체크 → 가까우면 재공격, 멀어졌으면 ChaseState 복귀.
/// 
/// 미래 (Step 6-10): 공격 도중 Animation Event 가 EnemyAttacker.PerformHit 호출.
/// 지금은 모션 + 상태 전환만.
/// </summary>
public class EnemyAttackState : EnemyStateBase
{
    public EnemyAttackState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // 1. 이동 정지 (공격 도중 NavMeshAgent 영향 차단)
        _stateMachine.Movement.StopMoving();

        // 2. Target 향함 (1회 즉시 회전, 다크소울 표준)
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
        }

        // 3. 공격 애니메이션 재생 (IsAttackFinished 플래그 리셋)
        _stateMachine.Animator.PlayAttack();

        // 4. Animator MoveSpeed 0 (Idle 자세로, Locomotion 영향 안 받음)
        _stateMachine.Animator.SetMoveSpeed(0f);
    }

    public override void OnUpdate()
    {
        // Target null 안전망
        if (_stateMachine.Target == null) return;

        // 공격 종료 감지 (Animation Event 가 IsAttackFinished = true 설정)
        if (_stateMachine.Animator.IsAttackFinished)
        {
            HandleAttackFinished();
        }

        // 공격 도중에는 회전/이동 처리 없음 (본인 결정: 다크소울 표준)
    }

    /// <summary>
    /// 공격 종료 후 거리 분기.
    /// 가까움: AttackState 유지 + 재공격 (PlayAttack 직접 호출, ChangeState 안 함).
    /// 멀어짐: ChaseState 복귀.
    /// 한 프레임 ChaseState 거치는 함정 회피 (본인 결정).
    /// </summary>
    private void HandleAttackFinished()
    {
        float distance = _stateMachine.Movement.DistanceTo(_stateMachine.Target.position);

        if (distance < _stateMachine.AttackRange)
        {
            // 가까움: AttackState 유지 + 재공격
            // ChangeState 안 함 (같은 상태 무시 + 한 프레임 함정 회피).
            // OnEnter 의 회전 + PlayAttack 만 다시 수행.

            // Target 향함 (다음 공격도 1회 회전)
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);

            // 새 공격 시작
            _stateMachine.Animator.PlayAttack();
        }
        else
        {
            // 멀어짐: 추격 복귀 (파생이 대상 결정)
            _stateMachine.ToChase();
        }
    }
}