using UnityEngine;

/// <summary>
/// 보스 근접 패턴의 공통 흐름. 정지 → 회전 → 패턴 데미지 설정 → 클립 재생 →
/// 애니 종료(IsAttackFinished) → 공격 쿨다운 + Recovery.
/// 패턴별 고유값(클립 인덱스/데미지/회복)은 파생이 정의하고,
/// 고유 동작(넉백/전진 등)은 OnAttackStart/OnAttackUpdate 훅으로 추가.
/// </summary>
public abstract class BossMeleeAttackStateBase : EnemyStateBase
{
    protected readonly BossStateMachine _boss;
    protected BossAttacker _attacker;

    // === 패턴별 고유값 (파생이 정의) ===
    protected abstract int AttackIndex { get; }     // 애니 ComboIndex (발차기=0, 베기=1)
    protected abstract int Damage { get; }
    protected abstract float RecoveryTime { get; }

    protected BossMeleeAttackStateBase(BossStateMachine stateMachine) : base(stateMachine)
    {
        _boss = stateMachine;
    }

    public override void OnEnter()
    {
        if (_attacker == null) _attacker = _stateMachine.GetComponent<BossAttacker>();

        _stateMachine.Movement.StopMoving();
        _stateMachine.Animator.SetMoveSpeed(0f);

        // 공격 방향 1회 고정 (윈드업 중 회전 안 함 → 회피 여지)
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
        }

        // 이 패턴의 데미지 주입 후 해당 클립 재생 (OnAttackHit 이벤트가 PerformHit 호출)
        _attacker?.SetAttackDamage(Damage);
        _stateMachine.Animator.PlayComboAttack(AttackIndex);

        OnAttackStart();   // 패턴 고유 진입 동작 (기본 빈)
    }

    public override void OnUpdate()
    {
        OnAttackUpdate();  // 패턴 고유 매프레임 동작 (기본 빈)

        // 애니 종료 → 공격 쿨다운 걸고 회복으로
        if (_stateMachine.Animator.IsAttackFinished)
        {
            _boss.StartScaledAttackCooldown();
            _boss.ToRecovery(RecoveryTime);
        }
    }

    // === 패턴 고유 훅 (옵션 오버라이드) ===
    protected virtual void OnAttackStart() { }
    protected virtual void OnAttackUpdate() { }
}