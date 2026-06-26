using UnityEngine;

/// <summary>
/// 중립 허브: 간격(거리)을 읽고 다음 수를 결정. 지금은 스포크가 없어 "접근"만 한다.
/// 행동 스포크(Bow/Dash/Combo/Parry)는 OnUpdate 의 결정 분기에 단계별로 추가.
/// </summary>
public class BossNeutralState : EnemyStateBase
{
    private readonly BossStateMachine _boss;
    private readonly BossConfig _config;   // ← 추가

    public BossNeutralState(BossStateMachine stateMachine, BossConfig config) : base(stateMachine)
    {
        _boss = stateMachine;
        _config = config;   // ← 추가
    }

    public override void OnEnter()
    {
        // 근접 사거리에서 멈춤 (행동 결정은 OnUpdate). 스포크 추가 시 거리별 분기.
        _stateMachine.Movement.SetStoppingDistance(_stateMachine.AttackRange);
    }

    public override void OnUpdate()
    {
        if (_stateMachine.Target == null) return;

        float distance = _stateMachine.Movement.DistanceTo(_stateMachine.Target.position);

        // ====== 행동 결정 (스포크 추가 지점) ======
        // [Bow]   중거리 + IsBowReady          → _boss.ToBow()    (스포크1)
        // [Dash]  간격 조절                     → _boss.ToDash()   (스포크2)
        // [Combo] 근접 + IsAttackReady          → _boss.ToCombo()  (스포크3)
        // [Parry] 근접 예측 (페이즈2)            → _boss.ToParry()  (스포크4)
        // 지금은 스포크 없음 → 접근만.
        // ==========================================

        // [Bow] 중거리 + 활 준비 + 시야 → 활 발사
        if (_boss.IsBowReady
            && _config != null
            && distance >= _config.BowMinDistance
            && distance <= _config.BowMaxDistance
            && _stateMachine.HasLineOfSightToTarget())
        {
            _boss.ToBow();
            return;
        }

        // [Melee/Guard] 근접 도달 + 공격 준비 → 가끔 가드, 아니면 발차기/베기
        if (_stateMachine.CanAttackTarget()
            && _stateMachine.IsAttackReady)
        {
            // 가드 준비 + 확률 → 반사 가드 (예측 불가성: 공격인지 가드인지 모름)
            if (_boss.IsGuardReady
                && _config != null
                && Random.value < _config.GuardChance)
            {
                _boss.ToGuard();
            }
            else if (Random.value < 0.5f)
            {
                _boss.ToKick();
            }
            else
            {
                _boss.ToSlash();
            }
            return;
        }

        // 접근: Target 으로 이동 (NavMesh 가 벽 우회). 근접하면 stoppingDistance 에서 멈춤.
        _stateMachine.Movement.MoveTo(
            _stateMachine.Target.position,
            _stateMachine.Movement.ChaseSpeed * _boss.SpeedScale   // ← 페이즈2 접근 가속
        );

        UpdateAnimatorMoveSpeed();
    }

    /// <summary>실제 속도 → Animator MoveSpeed (EnemyChaseState 와 동일 방식).</summary>
    private void UpdateAnimatorMoveSpeed()
    {
        float actual = _stateMachine.Movement.CurrentSpeed;
        float normalized = Mathf.Clamp01(actual / _stateMachine.Movement.ChaseSpeed);
        _stateMachine.Animator.SetMoveSpeed(normalized);
    }
}