using UnityEngine;

/// <summary>
/// 보스 전용 범위 공격 - 점프 내려찍기 (페이즈 2, 근거리).
/// 제자리에서 예비 후 내려찍어 보스 중심 원형 범위를 강타한다 (돌진의 직선/이동과 차별).
/// 
/// 단계 (이동 없음, 제자리):
/// - Windup(예비): 힘을 모음 (선딜). 타겟 바라봄.
/// - Slam(착지): 예비 끝 = 착지 시점에 PerformSlam 1회 (원형 범위 데미지).
/// - Recovery(후딜): 큰 공격 후 빈틈 (반격 기회). 끝나면 거리 분기.
/// 
/// 착지 판정은 코드 타이밍 (예비 종료 = 착지). 폴리싱 단계에 내려찍기 클립 +
/// 애니메이션 이벤트로 정교화 예정. Config 생성자 주입(BossConfig).
/// </summary>
public class BossSlamState : EnemyStateBase
{
    private enum Phase { Windup, Recovery }

    private readonly BossConfig _bossConfig;
    private BossAttacker _attacker;

    private Phase _phase;
    private float _timer;

    public BossSlamState(EnemyStateMachineBase stateMachine, BossConfig config)
        : base(stateMachine)
    {
        _bossConfig = config;
    }

    public override void OnEnter()
    {
        if (_bossConfig == null)
        {
            Debug.LogError("[BossSlamState] BossConfig is null. Returning to chase.");
            _stateMachine.ToChase();
            return;
        }

        if (_attacker == null)
        {
            _attacker = _stateMachine.GetComponent<BossAttacker>();
        }

        // 예비 동작 시작 (제자리 정지 + 타겟 바라봄)
        _phase = Phase.Windup;
        _timer = _bossConfig.SlamWindupTime;

        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
        }
        _stateMachine.Movement.StopMoving();  // 제자리 (이동 없음)
        _stateMachine.Animator.PlayLocomotion();
        _stateMachine.Animator.SetMoveSpeed(0f);
    }

    public override void OnUpdate()
    {
        if (_bossConfig == null) return;

        switch (_phase)
        {
            case Phase.Windup: UpdateWindup(); break;
            case Phase.Recovery: UpdateRecovery(); break;
        }
    }

    /// <summary>예비: 힘 모으기. 끝나는 순간 = 착지 → PerformSlam 1회 → 후딜로.</summary>
    private void UpdateWindup()
    {
        _stateMachine.Animator.SetMoveSpeed(0f);  // 제자리 유지

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        // 착지 시점: 원형 범위 강타 (1회)
        if (_attacker != null)
        {
            _attacker.PerformSlam(_bossConfig.SlamDamage);
        }

        // 후딜 진입
        _phase = Phase.Recovery;
        _timer = _bossConfig.SlamRecoveryTime;
    }

    /// <summary>후딜: 빈틈. 끝나면 거리 분기.</summary>
    private void UpdateRecovery()
    {
        _stateMachine.Animator.SetMoveSpeed(0f);

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        // 후딜 종료 → 거리 분기
        if (_stateMachine.Target == null)
        {
            _stateMachine.ToChase();
            return;
        }

        // 패턴 종료 → 쿨다운 대기
        ((BossStateMachine)_stateMachine).ToCooldown();
    }
}