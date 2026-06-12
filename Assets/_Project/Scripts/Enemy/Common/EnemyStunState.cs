using UnityEngine;

/// <summary>
/// 적 경직 상태 (패리 성공 시 등). 지정 시간 동안 행동 불능으로 멈추고,
/// 시간이 끝나면 추격(ToChase)으로 복귀한다 (경직은 전투 중에만 발생).
/// 지속 시간은 진입 전 SetDuration 으로 주입 (EnterParriedStun 이 설정).
/// 모든 적 공통 (베이스 타입 _stateMachine 만 사용).
/// </summary>
public class EnemyStunState : EnemyStateBase
{
    private float _duration;
    private float _endTime;

    public EnemyStunState(EnemyStateMachineBase stateMachine) : base(stateMachine) { }

    /// <summary>경직 지속 시간 설정 (진입 전 호출).</summary>
    public void SetDuration(float duration)
    {
        _duration = duration;
    }

    public override void OnEnter()
    {
        _endTime = Time.time + _duration;

        // 행동 불능: 이동 정지
        _stateMachine.Movement.StopMoving();

        // 경직 애니메이션 (비틀거림 루프)
        _stateMachine.Animator.SetStunned(true);
    }

    public override void OnUpdate()
    {
        // 경직 종료 → 추격 복귀 (플레이어와 교전 중이었으므로)
        if (Time.time >= _endTime)
        {
            _stateMachine.ToChase();
        }
    }

    public override void OnExit()
    {
        _stateMachine.Animator.SetStunned(false);
    }
}