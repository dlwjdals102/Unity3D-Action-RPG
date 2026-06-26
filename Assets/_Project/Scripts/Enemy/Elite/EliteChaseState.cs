using UnityEngine;

/// <summary>
/// 엘리트 적의 추격 상태. EnemyChaseState 를 상속하여 "돌진 발동 판단" 만 추가한다.
/// 나머지(공격 거리 도달, 추격 포기, 이동, Animator)는 부모(EnemyChaseState) 재사용.
/// 
/// 돌진 발동: 중거리(ChargeMin ~ ChargeMax) + 돌진 쿨다운 OK → ToCharge.
/// - 너무 가까움(< AttackRange): 부모가 ToAttack (콤보)
/// - 중거리 + 쿨다운 OK: ToCharge (돌진)
/// - 중거리 + 쿨다운 중 or 그 외: 부모가 추격/이동
/// 
/// 돌진 쿨다운은 EliteEnemyStateMachine 이 소유(IsChargeReady/StartChargeCooldown).
/// EliteEnemyStateMachine 을 직접 참조 (엘리트 전용이라 정당, ToCharge 는 엘리트 전용 메서드).
/// </summary>
public class EliteChaseState : EnemyChaseState
{
    private readonly EliteEnemyStateMachine _eliteSM;
    private readonly EliteEnemyConfig _eliteConfig;

    public EliteChaseState(EliteEnemyStateMachine stateMachine, EliteEnemyConfig config)
        : base(stateMachine)
    {
        _eliteSM = stateMachine;
        _eliteConfig = config;
    }

    public override void OnUpdate()
    {
        // Config 누락 시 부모 추격만 (돌진 없이)
        if (_eliteConfig == null)
        {
            base.OnUpdate();
            return;
        }

        if (_stateMachine.Target == null)
        {
            base.OnUpdate();
            return;
        }

        float distance = _stateMachine.Movement.DistanceTo(_stateMachine.Target.position);

        // 돌진 발동 판단: 중거리 + 쿨다운 OK
        bool inChargeRange = distance >= _eliteConfig.ChargeMinDistance
                          && distance <= _eliteConfig.ChargeMaxDistance;
        bool cooldownReady = _eliteSM.IsChargeReady;

        if (inChargeRange && cooldownReady)
        {
            // 돌진 쿨다운 시작 (다음 돌진 가능 시각)
            _eliteSM.StartChargeCooldown();
            _eliteSM.ToCharge();
            return;
        }

        // 돌진 안 하면 부모 로직 (공격 거리 → 콤보, 추격 포기 → 순찰, 이동)
        base.OnUpdate();
    }
}