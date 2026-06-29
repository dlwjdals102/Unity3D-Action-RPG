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
/// 미래 파생 예시: MeleeEnemyStateMachine (근접), RangedEnemyStateMachine (원거리),
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

    [Header("Vision")]
    [Tooltip("시야 차단 판정용 장애물(벽) 레이어")]
    [SerializeField] protected LayerMask _obstacleLayer;
    [Tooltip("시야 레이캐스트 눈높이 (바닥에서)")]
    [SerializeField] protected float _eyeHeight = 1.5f;

    // === Patrol Points ===
    [Header("Patrol")]
    [Tooltip("순찰 경로 (순서대로 방문). 비어있으면 PatrolState 가 Idle 상태로 머무름")]
    [SerializeField] protected Transform[] _patrolPoints;

    // 패트롤 포인트(자식)의 월드 좌표를 시작 시점에 스냅샷 → 이후 자식이 적을 따라가도 순찰 경로는 고정
    private Vector3[] _patrolPositions;

    // === Component References (각 상태가 접근) ===
    public EnemyMovement Movement { get; private set; }
    public EnemyAnimator Animator { get; private set; }

    // 사망/피격 이벤트 구독용 (내부 보유)
    protected EnemyHealth _health;

    private Vector3 _initialPosition;
    private Quaternion _initialRotation;

    // === Public Accessors (상태가 접근) ===
    /// <summary>적이 추격할 대상. ChaseState/AttackState 가 거리/방향 계산에 활용.</summary>
    public Transform Target => _target;

    /// <summary>순찰 경로. PatrolState 가 활용.</summary>
    /*public Transform[] PatrolPoints => _patrolPoints;*/

    /// <summary>순찰 경로(시작 시점 고정 월드 좌표). PatrolState 가 활용.</summary>
    public Vector3[] PatrolPositions => _patrolPositions;

    /// <summary>공격 거리. ChaseState 와 AttackState 가 활용.</summary>
    public float AttackRange => _config != null ? _config.AttackRange : 0f;

    /// <summary>추격 포기 거리. ChaseState 가 활용.</summary>
    public float GiveUpRange => _config != null ? _config.GiveUpRange : 0f;

    /// <summary>공격 후 대기 시간(초). 모든 적 공통. AttackState 들이 활용.</summary>
    public float AttackCooldown => _config != null ? _config.AttackCooldown : 0f;

    // === 공격 쿨다운 (중앙화, Time.time 기준) ===
    // 보스가 쓰던 _nextAttackTime 패턴을 베이스로 끌어올려 모든 적 공통화.
    // AttackState 들은 IsAttackReady 로 발동 여부를 판단하고,
    // 공격 후 StartAttackCooldown() 으로 다음 가능 시각을 갱신한다.

    /// <summary>다음 공격 가능 시각 (Time.time 기준). 상태 사이클을 넘어 유지.</summary>
    protected float _nextAttackTime;

    /// <summary>지금 공격 가능한가 (쿨다운 경과 여부).</summary>
    public bool IsAttackReady => Time.time >= _nextAttackTime;

    /// <summary>공격 직후 호출. AttackCooldown 만큼 다음 공격을 지연.</summary>
    public void StartAttackCooldown() => _nextAttackTime = Time.time + AttackCooldown;

    /// <summary>
    /// 전투 진입 시(Patrol 이탈) 호출. 공격 쿨다운을 걸어 첫 공격을 한 박자 늦춘다.
    /// 파생(Elite)은 override 로 추가 쿨다운(돌진)도 함께 건다.
    /// </summary>
    public virtual void StartCombatCooldowns() => StartAttackCooldown();

    // === Current State ===
    public EnemyStateBase CurrentState { get; private set; }

    /// <summary>경직 상태 (공통). 패리 성공 시 EnterParriedStun 으로 진입.</summary>
    public EnemyStunState StunState { get; private set; }

    /// <summary>
    /// 전투 상태 여부 (순찰이 아니면 전투 중 = 플레이어를 감지해 추격/공격 중).
    /// 머리 위 체력바 표시 등에 사용. 파생 클래스가 자신의 PatrolState 와 비교해 구현.
    /// </summary>
    public virtual bool IsInCombat => false;

    /// <summary>
    /// 휴식(RespawnAll) 시 부활 대상인가. 일반 적은 true.
    /// 보스는 override 로 false - 소울 규칙상 한 번 죽으면 휴식으로 부활 안 함.
    /// </summary>
    public virtual bool ParticipatesInRespawn => true;

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

        StunState = new EnemyStunState(this);  // 공통 상태라 베이스가 직접 생성

        // 초기 위치/회전 기억 (화톳불 휴식 시 리스폰 복원용)
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;

        // 패트롤 자식 좌표를 지금 떠둔다 (_initialPosition 캡처와 같은 타이밍 가정)
        CachePatrolPositions();
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

    /// <summary>
    /// 같은 상태여도 OnExit → OnEnter 를 강제로 재실행하는 전환 (리셋/리스폰용).
    /// 일반 전환은 ChangeState(중복 무시)를 쓰고, 상태를 처음부터 다시 시작해야 하는
    /// 리셋 흐름에서만 사용한다. (예: 순찰 중인 적을 초기화 → 순찰을 처음부터 재시작)
    /// </summary>
    protected void ForceChangeState(EnemyStateBase newState)
    {
        CurrentState?.OnExit();

        CurrentState = newState;
        CurrentState.OnEnter();
    }

    /// <summary>
    /// 적을 초기 상태로 되돌린다 (화톳불 휴식 시 리스폰).
    /// 죽어서 비활성된 적도 다시 활성화하여 부활시킨다.
    /// 위치/회전 복원 + 체력 복구 + 콜라이더 재활성 + 초기 상태(순찰) 진입.
    /// </summary>
    public virtual void ResetToInitial()
    {
        // 1. 비활성(사망) 상태면 다시 활성화 → OnEnable 이 이벤트 재구독
        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
        }

        // 2. 위치/회전 복원 (NavMeshAgent 안전 이동) + Agent 상태 재개
        transform.rotation = _initialRotation;
        if (Movement != null)
        {
            Movement.Warp(_initialPosition);
            Movement.ResetAgent();  // isStopped/updatePosition 등 복구 (전투 중 멈춤 방지)
        }
        else
        {
            transform.position = _initialPosition;
        }

        // 3. 체력 복구
        if (_health != null) _health.ResetHealth();

        // 4. 콜라이더 재활성 (사망 시 비활성됐던 것)
        var col = GetComponent<Collider>();
        if (col != null) col.enabled = true;

        // 5. 사망 애니 리셋 + 초기 상태(순찰) 재진입
        // ChangeState 는 같은 상태면 무시하므로, 이미 순찰 중이던 적은 OnEnter 가
        // 다시 실행되지 않아 (Warp + ResetPath 후) 새 목적지를 받지 못하고 멈춘다.
        // 리셋은 "처음부터 다시" 가 목적이므로 강제 재진입을 사용한다.
        if (Animator != null) Animator.ResetDeathState();
        ForceChangeState(GetInitialState());
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

        // 정면 각도 (너무 가까우면 각도 무시하고 통과)
        if (toTarget.sqrMagnitude >= 0.01f)
        {
            float angle = Vector3.Angle(forward, toTarget);
            if (angle >= _config.DetectionAngle / 2f) return false;
        }

        return HasLineOfSightToTarget();
    }

    /// <summary>
    /// 적 ↔ 대상 사이에 장애물(벽)이 없는지 = 시야 확보 여부.
    /// 거리/정면각도는 보지 않음. 이미 어그로된 적이 "벽에 가렸는지" 판정용
    /// (정면 콘이 필요 없음 - 벽 돌 때 적이 옆을 봐도 LOS 만 맞으면 공격 가능).
    /// </summary>
    public bool HasLineOfSightToTarget()
    {
        if (_target == null) return false;
        Vector3 eye = transform.position + Vector3.up * _eyeHeight;
        Vector3 targetEye = _target.position + Vector3.up * _eyeHeight;
        return !Physics.Linecast(eye, targetEye, _obstacleLayer);
    }

    /// <summary>
    /// 이 적이 지금 대상을 공격할 수 있는가 (진입·유지 공통 술어).
    /// 기본 = 사거리 안. 원거리 파생은 override 로 시야(LOS) 조건을 AND 한다.
    ///
    /// 핵심: Chase 진입 게이트와 Attack 유지 게이트가 같은 이 함수를 쓴다.
    /// → "들어갔는데 조건 깨져도 안 나오는" 비대칭 구멍이 구조적으로 불가능.
    /// </summary>
    public virtual bool CanAttackTarget()
    {
        if (_target == null) return false;
        return DistanceToTarget() < AttackRange;
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

    /// <summary>패리로 경직될 수 있는가. 보스는 false 로 오버라이드 (밸런스 보호).</summary>
    public virtual bool CanBeParried => true;

    /// <summary>패리 성공 시 경직 진입 (PlayerHealth 가 호출). 패리 불가/사망/이미 경직이면 무시.</summary>
    public void EnterParriedStun(float duration)
    {
        if (!CanBeParried) return;
        if (_health != null && _health.IsDead) return;
        if (CurrentState == StunState) return;

        StunState.SetDuration(duration);
        ChangeState(StunState);
    }

    /// <summary>
    /// _patrolPoints(자식 Transform)의 월드 좌표를 시작 시점에 캐싱한다.
    /// 자식으로 두면 프리팹에 함께 저장돼 배치 시 자동 연결되지만 런타임엔 적을 따라 움직이므로,
    /// 여기서 좌표만 고정해두고 PatrolState 가 이 좌표를 순찰한다. null 슬롯은 제외.
    /// </summary>
    private void CachePatrolPositions()
    {
        _patrolPositions = BuildPositionsFromPoints(_patrolPoints);
    }

    /// <summary>
    /// Transform 배열의 월드 좌표를 Vector3[] 로 추린다 (null 슬롯 제외). 캐싱/기즈모 공용.
    /// </summary>
    private static Vector3[] BuildPositionsFromPoints(Transform[] points)
    {
        if (points == null) return new Vector3[0];

        int count = 0;
        for (int i = 0; i < points.Length; i++)
            if (points[i] != null) count++;

        Vector3[] result = new Vector3[count];
        int idx = 0;
        for (int i = 0; i < points.Length; i++)
            if (points[i] != null) result[idx++] = points[i].position;
        return result;
    }

    // ========================================================================
    // === Editor Visualization (공통) ===
    // ========================================================================
    private void OnDrawGizmos()
    {
        // Patrol 경로 시각화 (노란 구체 + 선)
        // 플레이 중: 캐싱된 고정 좌표(실제 순찰 경로). 에디트 중: 자식 Transform 현재 좌표(작성용).
        Vector3[] gizmoPoints = Application.isPlaying ? _patrolPositions : BuildPositionsFromPoints(_patrolPoints);
        if (gizmoPoints != null && gizmoPoints.Length > 0)
        {
            Gizmos.color = Color.yellow;
            for (int i = 0; i < gizmoPoints.Length; i++)
            {
                Gizmos.DrawWireSphere(gizmoPoints[i], 0.5f);
                int nextIndex = (i + 1) % gizmoPoints.Length;
                Gizmos.DrawLine(gizmoPoints[i], gizmoPoints[nextIndex]);
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