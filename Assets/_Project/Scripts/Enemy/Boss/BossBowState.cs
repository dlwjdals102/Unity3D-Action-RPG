using UnityEngine;

/// <summary>
/// 활 발사 (이벤트 기반). 드로우 시작에 방향 고정(조준락) → 드로우 클립 재생 →
/// 클립의 OnBowRelease 이벤트가 화살 발사 → 클립 종료(IsAttackFinished) → Recovery.
/// 윈드업(회피창) 길이 = 클립의 릴리즈 프레임 위치. 근접 패턴과 동일한 이벤트 흐름.
/// </summary>
public class BossBowState : EnemyStateBase
{
    private readonly BossStateMachine _boss;
    private readonly BossConfig _config;
    private BossAttacker _attacker;

    public BossBowState(BossStateMachine stateMachine, BossConfig config) : base(stateMachine)
    {
        _boss = stateMachine;
        _config = config;
    }

    public override void OnEnter()
    {
        if (_attacker == null) _attacker = _stateMachine.GetComponent<BossAttacker>();

        _stateMachine.Movement.StopMoving();
        _stateMachine.Animator.SetMoveSpeed(0f);

        // 드로우 시작: Target 향해 1회 회전 + 발사 방향 고정 (릴리즈까지 회피창)
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
        }
        _attacker?.LockArrowAim(_stateMachine.Target);

        // 드로우 클립 재생 (OnBowRelease 이벤트가 발사, OnAttackAnimationEnd 가 종료)
        _stateMachine.Animator.PlayBow();
    }

    public override void OnUpdate()
    {
        // 클립 종료 → 활 쿨다운 걸고 회복으로 (발사는 이미 OnBowRelease 이벤트가 처리)
        if (_stateMachine.Animator.IsAttackFinished)
        {
            _boss.StartBowCooldown();
            _boss.ToRecovery(_config != null ? _config.BowRecoveryTime : 0.6f);
        }
    }
}