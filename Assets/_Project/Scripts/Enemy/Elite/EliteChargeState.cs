using UnityEngine;

/// <summary>
/// 엘리트 적의 돌진 상태.
/// 발동 시 방향을 고정하고 직선으로 빠르게 돌진 → 충돌 또는 최대 거리 도달 시 경직.
/// 직선이라 플레이어가 옆으로 피하면 빗나감 (회피 보상, 다크소울 돌진 표준).
/// 
/// 흐름 (두 모드):
/// - 돌진 모드: ChargeMove(직선) + PerformChargeHit(충돌) + 최대 거리 체크.
///   충돌(Player 맞춤) 또는 최대 거리 → 경직 진입.
/// - 경직 모드: 정지 + Idle + 경직 타이머. 끝나면 거리 분기 (콤보/추격).
/// 
/// 이동은 EnemyMovement 의 Begin/ChargeMove/EndCharge (transform 직접 이동 + NavMeshAgent 동기화).
/// Config 는 생성자 주입 (EliteEnemyConfig, EliteComboAttackState 패턴).
/// 애니메이션은 임시로 Run 모션 (폴리싱 단계에 돌진 전용 클립).
/// </summary>
public class EliteChargeState : EnemyStateBase
{
    private readonly EliteEnemyConfig _eliteConfig;
    private EliteEnemyAttacker _attacker;

    private Vector3 _chargeDirection;
    private Vector3 _startPosition;
    private bool _isStunned;
    private float _stunTimer;
    private bool _hasHit;
    private bool _isWindingUp;
    private float _windupTimer;

    public EliteChargeState(EnemyStateMachineBase stateMachine, EliteEnemyConfig config)
        : base(stateMachine)
    {
        _eliteConfig = config;
    }

    public override void OnEnter()
    {
        // Config 누락 가드 (폴백)
        if (_eliteConfig == null)
        {
            Debug.LogError("[EliteChargeState] EliteEnemyConfig is null. Returning to chase.");
            _stateMachine.ToChase();
            return;
        }

        // Attacker 캐싱 (첫 진입 1회)
        if (_attacker == null)
        {
            _attacker = _stateMachine.GetComponent<EliteEnemyAttacker>();
        }

        _isWindingUp = true;   // 예비동작부터 시작
        _isStunned = false;
        _hasHit = false;

        // 돌진 방향을 예비동작 시작 시점에 고정 (이후 추적 안 함 → 회피 보상)
        if (_stateMachine.Target != null)
        {
            _stateMachine.Movement.SetRotationImmediate(_stateMachine.Target.position);

            Vector3 dir = _stateMachine.Target.position - _stateMachine.transform.position;
            dir.y = 0;
            _chargeDirection = dir.sqrMagnitude > 0.01f ? dir.normalized : _stateMachine.transform.forward;
        }
        else
        {
            _chargeDirection = _stateMachine.transform.forward;
        }

        // 예비동작: 정지 + Idle. 실제 돌진(BeginCharge/Run)은 StartDash 에서.
        _windupTimer = _eliteConfig.ChargeWindupDuration;
        _stateMachine.Movement.StopMoving();
        _stateMachine.Animator.PlayLocomotion();
        _stateMachine.Animator.SetMoveSpeed(0f);
    }

    public override void OnUpdate()
    {
        if (_eliteConfig == null) return;

        // === 예비동작 모드 ===
        if (_isWindingUp)
        {
            UpdateWindup();
            return;
        }

        // === 경직 모드 ===
        if (_isStunned)
        {
            UpdateStun();
            return;
        }

        // === 돌진 모드 ===
        // 1. 충돌 감지 (Player 맞춤) - 1회만 데미지
        if (!_hasHit && _attacker != null)
        {
            if (_attacker.PerformChargeHit(_eliteConfig.ChargeDamage))
            {
                _hasHit = true;
                EnterStun();  // 맞췄으면 돌진 종료 → 경직
                return;
            }
        }

        // 2. 최대 거리 도달 체크 (빗나가도 일정 거리 후 멈춤)
        float traveled = Vector3.Distance(_startPosition, _stateMachine.transform.position);
        if (traveled >= _eliteConfig.ChargeMaxDistance)
        {
            EnterStun();
            return;
        }

        // 3. 직선 돌진 이동 - 벽(NavMesh 경계)에 막히면 종료 (벽 = 회복 punish 창)
        if (!_stateMachine.Movement.ChargeMove(_chargeDirection, _eliteConfig.ChargeSpeed))
        {
            EnterStun();
            return;
        }
    }

    /// <summary>
    /// 돌진 종료 → 경직 진입. NavMesh 복귀 + Idle.
    /// </summary>
    private void EnterStun()
    {
        _isStunned = true;
        _stunTimer = _eliteConfig.ChargeStunDuration;

        // 돌진 이동 모드 종료 (NavMesh 위치 복원)
        _stateMachine.Movement.EndCharge();

        // Idle 자세 (경직 표현은 폴리싱 단계, 지금은 Idle)
        _stateMachine.Animator.PlayLocomotion();
        _stateMachine.Animator.SetMoveSpeed(0f);
    }

    /// <summary>
    /// 경직 중 매 프레임. 타이머 종료 시 거리 분기.
    /// </summary>
    private void UpdateStun()
    {
        // Idle 유지 (매 프레임 MoveSpeed 0 댐핑 수렴)
        _stateMachine.Animator.SetMoveSpeed(0f);

        _stunTimer -= Time.deltaTime;
        if (_stunTimer > 0f) return;

        // 경직 종료 → 거리 분기
        if (_stateMachine.Target == null)
        {
            _stateMachine.ToChase();
            return;
        }

        float distance = _stateMachine.Movement.DistanceTo(_stateMachine.Target.position);

        if (distance <= _stateMachine.AttackRange)
        {
            // 가까움: 콤보
            _stateMachine.ToAttack();
        }
        else
        {
            // 멀음: 추격
            _stateMachine.ToChase();
        }
    }

    /// <summary>
    /// 예비동작 중 매 프레임. 방향 고정한 채 잠깐 정지(Idle) → 타이머 종료 시 실제 돌진.
    /// </summary>
    private void UpdateWindup()
    {
        // Idle 유지 (매 프레임 MoveSpeed 0 댐핑 수렴)
        _stateMachine.Animator.SetMoveSpeed(0f);

        _windupTimer -= Time.deltaTime;
        if (_windupTimer > 0f) return;

        StartDash();
    }

    /// <summary>
    /// 예비 종료 → 실제 직선 돌진 시작. BeginCharge + Run 모션 + 거리 기준점 설정.
    /// </summary>
    private void StartDash()
    {
        _isWindingUp = false;
        _startPosition = _stateMachine.transform.position;

        _stateMachine.Movement.BeginCharge();
        _stateMachine.Animator.PlayCharge();   // 전용 돌진 모션 (이전: PlayLocomotion + SetMoveSpeed(1f))
    }
}