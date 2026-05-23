using UnityEngine;

/// <summary>
/// 모든 적 상태머신의 추상 베이스 클래스.
/// 상태 전환 메커니즘 + 공통 컴포넌트 참조 + 시야 감지 + 이벤트 구독을 제공한다.
/// 
/// 베이스가 제공하는 것 (공통 메커니즘):
/// - ChangeState (상태 전환)
/// - Update (CurrentState.OnUpdate 위임)
/// - CanSeeTarget (시야 감지)
/// - 컴포넌트 참조 (Movement, Animator), Config, Target, PatrolPoints
/// - OnDeath/OnDamaged 이벤트 구독 + unsubscribe
/// 
/// 파생이 구현하는 것 (적 종류별 차이):
/// - 상태 인스턴스 생성 (근접/원거리 등 다름) - CreateStates
/// - 초기 상태 진입 - GetInitialState
/// - 사망/피격 시 전환 대상 (ChaseState 가 근접/원거리 다름) - HandleDeath/HandleDamaged
/// 
/// 미래 파생 예시: EnemyStateMachine (근접), RangedEnemyStateMachine (원거리),
///   SummonerStateMachine (소환), FlyingEnemyStateMachine (비행).
/// </summary>
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyAnimator))]
[RequireComponent(typeof(EnemyHealth))]
public abstract class EnemyStateMachineBase : MonoBehaviour
{
    // === Config ===
    [Header("Config")]
    [Tooltip("적의 수치 데이터 (공격 거리, 시야 등)")]
    [SerializeField] protected EnemyConfig _config;

    // === Target Reference (플레이어) ===
    [Header("Target")]
    [Tooltip("적이 추격할 대상 (보통 Player)")]
    [SerializeField] protected Transform _target;

    // === Patrol Points ===
    [Header("Patrol")]
    [Tooltip("순찰 경로 (순서대로 방문). 비어있으면 PatrolState 가 Idle 상태로 머무름")]
    [SerializeField] protected Transform[] _patrolPoints;

    // === Component References (각 상태가 접근) ===
    public EnemyMovement Movement { get; private set; }
    public EnemyAnimator Animator { get; private set; }

    // 사망/피격 이벤트 구독용 (내부 보유)
    protected EnemyHealth _health;

    // === Public Accessors (상태가 접근) ===
    /// <summary>적이 추격할 대상. ChaseState/AttackState 가 거리/방향 계산에 활용.</summary>
    public Transform Target => _target;

    /// <summary>순찰 경로. PatrolState 가 활용.</summary>
    public Transform[] PatrolPoints => _patrolPoints;

    /// <summary>공격 거리. ChaseState 와 AttackState 가 활용.</summary>
    public float AttackRange => _config != null ? _config.AttackRange : 0f;

    /// <summary>추격 포기 거리. ChaseState 가 활용.</summary>
    public float GiveUpRange => _config != null ? _config.GiveUpRange : 0f;

    // === Current State ===
    public EnemyStateBase CurrentState { get; private set; }

    // ========================================================================
    // === Unity Lifecycle (파생이 override 가능, base 호출 필수) ===
    // ========================================================================

    protected virtual void Awake()
    {
        // 공통 컴포넌트 참조
        Movement = GetComponent<EnemyMovement>();
        Animator = GetComponent<EnemyAnimator>();
        _health = GetComponent<EnemyHealth>();

        // Config 검증
        if (_config == null)
        {
            Debug.LogError($"[{GetType().Name}] EnemyConfig not assigned on {gameObject.name}!");
        }

        // Target 검증
        if (_target == null)
        {
            Debug.LogWarning($"[{GetType().Name}] Target not assigned on {gameObject.name}!");
        }

        // 파생이 자기 상태 인스턴스 생성
        CreateStates();
    }

    protected virtual void OnEnable()
    {
        // 사망/피격 이벤트 구독. OnEnable 에서 구독하여 SetActive 시나리오 안전.
        if (_health != null)
        {
            _health.OnDeath += HandleDeath;
            _health.OnDamaged += HandleDamaged;
        }
    }

    protected virtual void OnDisable()
    {
        // 메모리 누수 방지: OnDisable 에서 unsubscribe (필수)
        if (_health != null)
        {
            _health.OnDeath -= HandleDeath;
            _health.OnDamaged -= HandleDamaged;
        }
    }

    protected virtual void Start()
    {
        // 파생이 지정한 초기 상태로 진입
        ChangeState(GetInitialState());
    }

    protected virtual void Update()
    {
        // 현재 상태의 매 프레임 로직 실행
        CurrentState?.OnUpdate();
    }

    // ========================================================================
    // === State Transition (공통 메커니즘) ===
    // ========================================================================

    /// <summary>
    /// 다른 상태로 전환한다.
    /// 같은 상태로의 전환은 무시되며, OnExit → OnEnter 순서로 호출된다.
    /// </summary>
    public void ChangeState(EnemyStateBase newState)
    {
        if (newState == CurrentState) return;

        CurrentState?.OnExit();

        CurrentState = newState;
        CurrentState.OnEnter();
    }

    // ========================================================================
    // === Abstract Members (파생이 구현) ===
    // ========================================================================

    /// <summary>
    /// 파생이 자기 상태 인스턴스를 생성한다 (Awake 에서 호출).
    /// 근접: Patrol/Chase/Attack/Death. 원거리: Patrol/RangedChase/RangedAttack/Death.
    /// </summary>
    protected abstract void CreateStates();

    /// <summary>
    /// 파생이 초기 진입 상태를 반환한다 (Start 에서 호출).
    /// 보통 PatrolState.
    /// </summary>
    protected abstract EnemyStateBase GetInitialState();

    /// <summary>
    /// 사망 이벤트 구독자. 파생이 자기 DeathState 로 전환.
    /// </summary>
    protected abstract void HandleDeath();

    /// <summary>
    /// 피격 이벤트 구독자. 파생이 자기 ChaseState 로 전환 (조건 포함).
    /// </summary>
    protected abstract void HandleDamaged();

    // ========================================================================
    // === State Transition Intents (상태가 호출, 파생이 대상 결정) ===
    // 상태는 "다음 상태 인스턴스" 가 아닌 "전환 의도" 를 호출한다.
    // 근접/원거리 파생이 각자의 상태로 전환 (PatrolState 가 ToChase 호출 시
    // 근접은 EnemyChaseState, 원거리는 RangedChaseState 로 감).
    // ========================================================================

    /// <summary>순찰 상태로 전환. ChaseState 등이 "추격 포기" 시 호출.</summary>
    public abstract void ToPatrol();

    /// <summary>추격 상태로 전환. PatrolState 의 시야 감지/피격 시 호출.</summary>
    public abstract void ToChase();

    /// <summary>공격 상태로 전환. ChaseState 의 공격 거리 도달 시 호출.</summary>
    public abstract void ToAttack();

    // ========================================================================
    // === Vision Helpers (공통) ===
    // ========================================================================

    /// <summary>
    /// Target 이 시야 안에 있는지 확인.
    /// 조건: (1) Target/Config 존재 (2) DetectionRange 안 (3) DetectionAngle 안 (정면 기준).
    /// 미래: 장애물 Raycast 추가 가능.
    /// </summary>
    public bool CanSeeTarget()
    {
        if (_target == null || _config == null) return false;

        Vector3 toTarget = _target.position - transform.position;
        float distance = toTarget.magnitude;

        if (distance > _config.DetectionRange) return false;

        toTarget.y = 0;
        Vector3 forward = transform.forward;
        forward.y = 0;

        if (toTarget.sqrMagnitude < 0.01f) return true;

        float angle = Vector3.Angle(forward, toTarget);
        return angle < _config.DetectionAngle / 2f;
    }

    /// <summary>
    /// Target 까지의 거리 (헬퍼). HandleDamaged 등이 활용.
    /// </summary>
    protected float DistanceToTarget()
    {
        if (_target == null) return float.MaxValue;
        return Vector3.Distance(transform.position, _target.position);
    }

    /// <summary>
    /// Config 의 DetectionRange 접근 (파생의 HandleDamaged 가 활용).
    /// </summary>
    protected float DetectionRange => _config != null ? _config.DetectionRange : 0f;

    // ========================================================================
    // === Editor Visualization (공통) ===
    // ========================================================================

    private void OnDrawGizmos()
    {
        // Patrol 경로 시각화 (노란 구체 + 선)
        if (_patrolPoints != null && _patrolPoints.Length > 0)
        {
            Gizmos.color = Color.yellow;

            for (int i = 0; i < _patrolPoints.Length; i++)
            {
                if (_patrolPoints[i] == null) continue;

                Gizmos.DrawWireSphere(_patrolPoints[i].position, 0.5f);

                int nextIndex = (i + 1) % _patrolPoints.Length;
                if (_patrolPoints[nextIndex] != null)
                {
                    Gizmos.DrawLine(_patrolPoints[i].position, _patrolPoints[nextIndex].position);
                }
            }
        }

        if (_config == null) return;

        // 공격 거리 (빨강)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _config.AttackRange);

        // 시야 감지 거리 (노랑)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _config.DetectionRange);

        // 추격 포기 거리 (주황)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, _config.GiveUpRange);

        // 시야각 (청록 선)
        Gizmos.color = Color.cyan;
        Vector3 forward = transform.forward * _config.DetectionRange;
        Quaternion leftRot = Quaternion.AngleAxis(-_config.DetectionAngle / 2f, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(_config.DetectionAngle / 2f, Vector3.up);
        Gizmos.DrawRay(transform.position, leftRot * forward);
        Gizmos.DrawRay(transform.position, rightRot * forward);
    }
}