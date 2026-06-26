using UnityEngine;

/// <summary>
/// 발차기 패턴. 빠르고 약한 견제. AttackIndex=0.
/// 고유 동작(넉백 등)은 OnAttackStart/OnAttackUpdate 에 추후 추가.
/// </summary>
public class BossKickState : BossMeleeAttackStateBase
{
    private readonly BossConfig _config;

    public BossKickState(BossStateMachine stateMachine, BossConfig config) : base(stateMachine)
    {
        _config = config;
    }

    protected override int AttackIndex => 0;
    protected override int Damage => _config != null ? _config.KickDamage : 12;
    protected override float RecoveryTime => _config != null ? _config.KickRecoveryTime : 0.4f;

    // 고유 동작 자리:
    // protected override void OnAttackStart() { /* 넉백 준비 등 */ }
}