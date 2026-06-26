using UnityEngine;

/// <summary>
/// 행동(콤보/대시/활/패리) 직후의 짧은 회복(빈틈) 상태. 플레이어의 반격 창.
/// 회복 시간이 지나면 Neutral 로 복귀해 간격을 다시 평가한다.
/// </summary>
public class BossRecoveryState : EnemyStateBase
{
    private readonly BossStateMachine _boss;
    private float _recoverEndTime;

    public BossRecoveryState(BossStateMachine stateMachine) : base(stateMachine)
    {
        _boss = stateMachine;
    }

    /// <summary>진입 직전 호출 (BossStateMachine.ToRecovery 가 설정).</summary>
    public void SetRecoveryDuration(float duration)
    {
        _recoverEndTime = Time.time + Mathf.Max(0f, duration);
    }

    public override void OnEnter()
    {
        _stateMachine.Movement.StopMoving();
        _stateMachine.Animator.SetMoveSpeed(0f);
        _stateMachine.Animator.PlayIdle();
    }

    public override void OnUpdate()
    {
        _stateMachine.Animator.SetMoveSpeed(0f);

        if (Time.time >= _recoverEndTime)
        {
            _boss.ToNeutral();
        }
    }
}