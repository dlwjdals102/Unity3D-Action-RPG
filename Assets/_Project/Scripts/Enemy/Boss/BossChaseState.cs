using UnityEngine;

/// <summary>
/// 보스의 추격 상태. EnemyChaseState 를 상속하여 "공격 타이밍 → 패턴 선택기 위임" 만 추가.
/// 나머지(추격 이동, 추격 포기, Animator)는 부모 재사용.
/// 
/// 일반 적은 AttackRange 안에서만 공격하지만, 보스는 돌진(원거리)도 있어
/// "공격 시작 거리(ChargeMaxDistance)" 안에 들어오면 패턴 선택기에 위임한다.
/// 어떤 패턴을 쓸지(페이즈/거리/가중치)는 BossStateMachine.TrySelectAndExecutePattern 이 결정.
/// (ChaseState 는 "공격할 타이밍인가" 만, "무엇을" 은 선택기가 - 책임 분리)
/// </summary>
public class BossChaseState : EnemyChaseState
{
    private readonly BossStateMachine _bossSM;
    private readonly BossConfig _bossConfig;

    public BossChaseState(BossStateMachine stateMachine, BossConfig config)
        : base(stateMachine)
    {
        _bossSM = stateMachine;
        _bossConfig = config;
    }

    public override void OnUpdate()
    {
        if (_bossConfig == null || _stateMachine.Target == null)
        {
            base.OnUpdate();
            return;
        }

        float distance = _stateMachine.Movement.DistanceTo(_stateMachine.Target.position);

        // 추격 포기 거리 초과 → 부모 (순찰 복귀)
        if (distance > _stateMachine.GiveUpRange)
        {
            base.OnUpdate();
            return;
        }

        // 근접(AttackRange)에 도달하면 패턴 선택기에 위임 (거리 무관 선택)
        // 약간의 여유(+0.5): NavMeshAgent 가 stoppingDistance 근처에서 멈출 때의 오차 흡수
        if (distance <= _stateMachine.AttackRange + 0.5f)
        {
            if (_bossSM.TrySelectAndExecutePattern())
            {
                return;  // 패턴 실행됨
            }
            // 선택 실패(쿨다운) → 계속 추격하며 대기 (아래 base)
        }

        // 그 외: 부모 추격 로직 (이동 + Animator)
        base.OnUpdate();
    }
}