using UnityEngine;

/// <summary>
/// 보스 전용 돌진. 엘리트 돌진(2단계: 돌진→경직)에 "예비 동작(Wind-up)"을 더한 3단계.
/// 예비 → 돌진 → 경직. 예비 동작이 묵직함과 회피 타이밍을 만든다 (보스다운 차별).
/// 
/// - 예비(Windup): 제자리에서 타겟을 바라보며 힘을 모음 (선딜). 이 사이 방향 고정.
/// - 돌진(Charge): 고정 방향 직선 돌진. 충돌(1회 데미지) 또는 최대 거리 → 경직.
/// - 경직(Stun): 큰 공격 후 빈틈. 끝나면 거리 분기.
/// 
/// 이동은 EnemyMovement 의 Begin/ChargeMove/EndCharge. Config 생성자 주입(BossConfig).
/// 애니메이션은 임시(Locomotion). 폴리싱 단계에 예비/돌진 전용 클립.
/// </summary>
public class BossChargeState : EnemyStateBase
{
    private enum Phase { Windup, Charging, Stunned }

    private readonly BossConfig _bossConfig;
    private BossAttacker _attacker;

    private Collider _bossCollider;
    private Collider _targetCollider;
    private bool _collisionIgnored;

    private Phase _phase;
    private float _timer;
    private Vector3 _chargeDirection;
    private Vector3 _startPosition;
    private bool _hasHit;

    public BossChargeState(EnemyStateMachineBase stateMachine, BossConfig config)
        : base(stateMachine)
    {
        _bossConfig = config;
    }

    public override void OnEnter()
    {
        if (_bossConfig == null)
        {
            Debug.LogError("[BossChargeState] BossConfig is null. Returning to chase.");
            _stateMachine.ToChase();
            return;
        }

        if (_attacker == null)
        {
            _attacker = _stateMachine.GetComponent<BossAttacker>();
        }

        // 예비 동작 시작
        _phase = Phase.Windup;
        _timer = _bossConfig.ChargeWindupTime;
        _hasHit = false;

        // 예비 중 타겟 바라봄 (방향 고정 준비) + 정지
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
        }
        _stateMachine.Animator.PlayLocomotion();
        _stateMachine.Animator.SetMoveSpeed(0f);  // 예비 중 정지
    }

    public override void OnUpdate()
    {
        if (_bossConfig == null) return;

        switch (_phase)
        {
            case Phase.Windup: UpdateWindup(); break;
            case Phase.Charging: UpdateCharging(); break;
            case Phase.Stunned: UpdateStun(); break;
        }
    }

    /// <summary>예비 동작: 제자리에서 힘 모으기. 끝나면 방향 고정 후 돌진 시작.</summary>
    private void UpdateWindup()
    {
        _stateMachine.Animator.SetMoveSpeed(0f);  // 정지 유지

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        // 예비 종료 → 돌진 방향 고정 (이 순간의 타겟 방향, 이후 직선 - 회피 보상)
        _startPosition = _stateMachine.transform.position;
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);
            Vector3 dir = _stateMachine.Target.position - _stateMachine.transform.position;
            dir.y = 0f;
            _chargeDirection = dir.sqrMagnitude > 0.01f ? dir.normalized : _stateMachine.transform.forward;
        }
        else
        {
            _chargeDirection = _stateMachine.transform.forward;
        }

        _phase = Phase.Charging;
        _stateMachine.Movement.BeginCharge();
        _stateMachine.Animator.SetMoveSpeed(1f);  // 돌진 모션

        // 돌진 중 보스 플레이어 물리 충돌 무시 (관통 밀어내지 않고 지나감)
        SetCollisionIgnored(true);
    }

    /// <summary>돌진: 고정 방향 직선. 충돌 또는 최대 거리 → 경직.</summary>
    private void UpdateCharging()
    {
        // 충돌 감지 (1회 데미지)
        if (!_hasHit && _attacker != null)
        {
            if (_attacker.PerformChargeHit(_bossConfig.ChargeDamage))
            {
                _hasHit = true;
            }
        }

        // 최대 거리 도달
        float traveled = Vector3.Distance(_startPosition, _stateMachine.transform.position);
        if (traveled >= _bossConfig.ChargeMaxDistance)
        {
            EnterStun();
            return;
        }

        // 직선 돌진 이동
        _stateMachine.Movement.ChargeMove(_chargeDirection, _bossConfig.ChargeSpeed);
    }

    private void EnterStun()
    {
        _phase = Phase.Stunned;
        _timer = _bossConfig.ChargeStunDuration;
        _stateMachine.Movement.EndCharge();
        _stateMachine.Animator.SetMoveSpeed(0f);

        // 충돌 무시 해제 (돌진 끝 → 다시 물리 충돌)
        SetCollisionIgnored(false);
    }

    /// <summary>경직: 빈틈 끝나면 거리 분기</summary>
    private void UpdateStun()
    {
        _stateMachine.Animator.SetMoveSpeed(0f);

        _timer -= Time.deltaTime;
        if (_timer > 0f) return;

        if (_stateMachine.Target == null)
        {
            _stateMachine.ToChase();
            return;
        }

        // 패턴 종료 → 쿨다운 대기 (쿨다운 후 선택기가 다음 패턴 결정)
        ((BossStateMachine)_stateMachine).ToCooldown();
    }

    /// <summary>
    /// 돌진 중 보스 플레이어 물리 충돌을 무시/복구한다 (관통 효과).
    /// 데미지는 OverlapSphere 라 영향 없고, Collider 간 밀어내는 물리만 차단.
    /// </summary>
    private void SetCollisionIgnored(bool ignored)
    {
        // 보스 Collider 캐싱 (1회)
        if (_bossCollider == null)
        {
            _bossCollider = _stateMachine.GetComponent<Collider>();
        }

        // 타겟(플레이어) Collider — 타겟이 바뀔 수 있으니 매번 확인
        if (_stateMachine.Target != null)
        {
            _targetCollider = _stateMachine.Target.GetComponent<Collider>();
        }

        if (_bossCollider == null || _targetCollider == null) return;

        Physics.IgnoreCollision(_bossCollider, _targetCollider, ignored);
        _collisionIgnored = ignored;
    }

    public override void OnExit()
    {
        // 어떤 경로로 나가든 충돌 무시는 반드시 복구 (관통 상태 잔류 방지)
        if (_collisionIgnored)
        {
            SetCollisionIgnored(false);
        }
    }
}