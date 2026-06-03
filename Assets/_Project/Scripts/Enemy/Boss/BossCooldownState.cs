using UnityEngine;

/// <summary>
/// 보스 패턴 사이의 쿨다운 대기 상태. 모든 공격 패턴(기본/돌진/내려찍기)이 끝나면 여기로 모인다.
/// 
/// 핵심: ChaseState(추격 모션)를 거치지 않아 "공격→추격→공격" 시의 1프레임 둠칫(Idle→Run→Idle
/// 깜빡임)을 피한다. 제자리에서 Idle + Target 조준을 유지하며 쿨다운만 센다.
/// 
/// - 쿨다운 중 멀어지면 → ChaseState (간격이 벌어졌으니 추격)
/// - 쿨다운 끝 + 공격 가능 거리 → 선택기 직접 호출 → 다음 패턴 (둠칫 없이 바로)
/// - 쿨다운 끝 + 너무 멀면 → ChaseState
/// 
/// (원래 MeleeEnemyAttackState 가 자기 안에서 하던 쿨다운 로직을 보스용 별도 State 로 분리.
///  쿨다운 끝에 "재공격" 대신 "선택기 호출"로 바꿔 패턴 다양성을 얻는다.)
/// </summary>
public class BossCooldownState : EnemyStateBase
{
    private readonly BossStateMachine _bossSM;
    private readonly BossConfig _bossConfig;
    private float _timer;

    public BossCooldownState(BossStateMachine stateMachine, BossConfig config)
        : base(stateMachine)
    {
        _bossSM = stateMachine;
        _bossConfig = config;
    }

    public override void OnEnter()
    {
        _timer = _stateMachine.AttackCooldown;
        _stateMachine.Movement.StopMoving();  // 제자리 (추격 안 함)
    }

    public override void OnUpdate()
    {
        if (_stateMachine.Target == null)
        {
            _stateMachine.ToChase();
            return;
        }

        // 제자리 Idle + Target 조준 (둠칫 방지: 추격 모션 대신 Idle 유지)
        _stateMachine.Animator.PlayIdle();
        _stateMachine.Movement.LookAt(_stateMachine.Target.position);

        _timer -= Time.deltaTime;

        // 쿨다운 끝 → 항상 추격으로 복귀 (근접까지 다시 붙은 뒤 ChaseState 가 패턴 선택)
        // 패턴 선택을 "근접 도달 시점"으로 일원화: CooldownState 는 거리 판단/선택을 하지 않는다.
        if (_timer <= 0f)
        {
            _stateMachine.ToChase();
        }
    }
}