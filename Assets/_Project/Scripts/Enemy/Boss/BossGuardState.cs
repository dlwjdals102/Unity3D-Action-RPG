using UnityEngine;

/// <summary>
/// 반사 가드. 막기 자세를 켜고(피격 무효) 가드 모션이 재생되는 동안:
/// - 플레이어가 때리면 → 무효 + 발차기 반격(ToKick)
/// - 안 때리면 → 모션 종료(IsAttackFinished) 시 공격 쿨다운 걸고 Neutral 복귀
/// "피격이 곧 신호" + "클립 끝 = 상태 끝" (공격/활과 동일한 종료 방식). 가드 로직이 이 상태에 응집.
/// </summary>
public class BossGuardState : EnemyStateBase
{
    private readonly BossStateMachine _boss;
    private EnemyHealth _health;

    public BossGuardState(BossStateMachine stateMachine, BossConfig config) : base(stateMachine)
    {
        _boss = stateMachine;
    }

    public override void OnEnter()
    {
        if (_health == null) _health = _stateMachine.GetComponent<EnemyHealth>();

        _stateMachine.Movement.StopMoving();
        _stateMachine.Animator.SetMoveSpeed(0f);

        // 플레이어 향해 막기 (가드 방향)
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
        }

        _health?.SetBlocking(true);          // 이 순간부터 피격 무효 + 기록
        _stateMachine.Animator.PlayGuard();  // 막기 스탠스 (1회 재생, 끝에 OnAttackAnimationEnd)
    }

    public override void OnUpdate()
    {
        // 막은 피격 → 즉시 발차기 반격 (반사)
        if (_health != null && _health.ConsumeBlockedHit())
        {
            _boss.ToKick();   // OnExit 가 SetBlocking(false) 처리
            return;
        }

        // 가드 모션 종료 → 공격 쿨다운(페이즈 반영) 걸고 Neutral 복귀
        if (_stateMachine.Animator.IsAttackFinished)
        {
            _boss.StartScaledAttackCooldown();
            _boss.ToNeutral();
        }
    }

    public override void OnExit()
    {
        _health?.SetBlocking(false);   // 모든 이탈(반격/복귀/사망)에서 가드 해제 보장
    }
}