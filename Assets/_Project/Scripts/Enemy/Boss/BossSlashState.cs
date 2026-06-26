using UnityEngine;

/// <summary>
/// 베기 패턴. 느리고 강한 한 방. AttackIndex=1. 긴 회복 → 반격 창.
/// 고유 동작(전진 등)은 OnAttackStart/OnAttackUpdate 에 추후 추가.
/// </summary>
public class BossSlashState : BossMeleeAttackStateBase
{
    private readonly BossConfig _config;

    public BossSlashState(BossStateMachine stateMachine, BossConfig config) : base(stateMachine)
    {
        _config = config;
    }

    protected override int AttackIndex => 1;
    protected override int Damage => _config != null ? _config.SlashDamage : 25;
    protected override float RecoveryTime => _config != null ? _config.SlashRecoveryTime : 0.8f;

    // 고유 동작 자리:
    // protected override void OnAttackUpdate() { /* 휘두르며 전진 등 */ }
}