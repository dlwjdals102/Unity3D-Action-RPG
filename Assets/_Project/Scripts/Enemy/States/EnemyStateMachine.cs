using UnityEngine;

/// <summary>
/// 적의 상태머신 중앙 관리자.
/// 현재 상태를 추적하고 상태 전환을 처리한다.
/// 각 상태는 이 클래스를 통해 다른 컴포넌트 (Movement, Animator) 와 Target (플레이어) 에 접근한다.
/// 
/// PlayerStateMachine 과 같은 패턴이지만 단순:
/// - 입력 출처가 사용자가 아닌 AI 결정 (거리, 시야 등)
/// - 컴포넌트 참조 적음 (Movement, Animator 만)
/// - Target (플레이어) 참조로 거리/방향 계산
/// - 시야 감지 헬퍼 메서드 제공 (모든 상태가 활용)
/// - EnemyHealth 의 OnDeath/OnDamaged 이벤트 구독 (사망 시 Death, 피격 시 추격)
/// </summary>
[RequireComponent(typeof(EnemyMovement))]
[RequireComponent(typeof(EnemyAnimator))]
[RequireComponent(typeof(EnemyHealth))]
public class EnemyStateMachine : MonoBehaviour
{
    // === Component References (각 상태가 접근) ===
    public EnemyMovement Movement { get; private set; }
    public EnemyAnimator Animator { get; private set; }

    // 사망/피격 이벤트 구독용 (내부 보유, 외부 노출 불필요)
    private EnemyHealth _health;

    // === Target Reference (플레이어) ===
    [Header("Target")]
    [Tooltip("적이 추격할 대상 (보통 Player)")]
    [SerializeField] private Transform _target;

    /// <summary>적이 추격할 대상. ChaseState/AttackState 가 거리/방향 계산에 활용.</summary>
    public Transform Target => _target;

    // === Patrol Points ===
    [Header("Patrol")]
    [Tooltip("순찰 경로 (순서대로 방문). 비어있으면 PatrolState 가 Idle 상태로 머무름")]
    [SerializeField] private Transform[] _patrolPoints;

    /// <summary>순찰 경로. PatrolState 가 활용.</summary>
    public Transform[] PatrolPoints => _patrolPoints;

    // === Combat Settings ===
    [Header("Combat")]
    [Tooltip("공격 거리. ChaseState 에서 이 거리 이내면 AttackState 전환")]
    [SerializeField] private float _attackRange = 1.5f;

    /// <summary>공격 거리. ChaseState 와 AttackState 가 활용.</summary>
    public float AttackRange => _attackRange;

    // === Vision (시야 감지) ===
    [Header("Vision")]
    [Tooltip("시야 감지 거리. PatrolState 에서 Target 이 이 거리 안 + 시야각 안이면 ChaseState 전환")]
    [SerializeField] private float _detectionRange = 8f;

    [Tooltip("시야각 (총 각도, 정면 기준 좌우 절반씩). 90도 = 좌우 45도")]
    [SerializeField] private float _detectionAngle = 90f;

    [Tooltip("추격 포기 거리. ChaseState 에서 Target 이 이 거리 초과 시 PatrolState 복귀")]
    [SerializeField] private float _giveUpRange = 15f;

    /// <summary>추격 포기 거리. ChaseState 가 활용.</summary>
    public float GiveUpRange => _giveUpRange;

    // === State Instances ===
    public EnemyPatrolState PatrolState { get; private set; }
    public EnemyChaseState ChaseState { get; private set; }
    public EnemyAttackState AttackState { get; private set; }
    public EnemyDeathState DeathState { get; private set; }

    // === Current State ===
    public EnemyStateBase CurrentState { get; private set; }

    private void Awake()
    {
        // 컴포넌트 참조 가져오기
        Movement = GetComponent<EnemyMovement>();
        Animator = GetComponent<EnemyAnimator>();
        _health = GetComponent<EnemyHealth>();

        // Target 검증
        if (_target == null)
        {
            Debug.LogWarning($"[EnemyStateMachine] Target not assigned on {gameObject.name}!");
        }

        // 상태 인스턴스 생성
        PatrolState = new EnemyPatrolState(this);
        ChaseState = new EnemyChaseState(this);
        AttackState = new EnemyAttackState(this);
        DeathState = new EnemyDeathState(this);
    }

    private void OnEnable()
    {
        // 사망/피격 이벤트 구독. OnEnable 에서 구독하여 SetActive(true/false) 시나리오 안전.
        if (_health != null)
        {
            _health.OnDeath += HandleDeath;
            _health.OnDamaged += HandleDamaged;
        }
    }

    private void OnDisable()
    {
        // 메모리 누수 방지: OnDisable 에서 unsubscribe (필수)
        if (_health != null)
        {
            _health.OnDeath -= HandleDeath;
            _health.OnDamaged -= HandleDamaged;
        }
    }

    private void Start()
    {
        // 초기 상태로 진입: Patrol
        ChangeState(PatrolState);
    }

    private void Update()
    {
        // 현재 상태의 매 프레임 로직 실행
        CurrentState?.OnUpdate();
    }

    /// <summary>
    /// 다른 상태로 전환한다.
    /// 같은 상태로의 전환은 무시되며, OnExit → OnEnter 순서로 호출된다.
    /// </summary>
    public void ChangeState(EnemyStateBase newState)
    {
        // 같은 상태로의 전환 무시 (반복 호출 방지)
        if (newState == CurrentState) return;

        // 이전 상태 종료
        CurrentState?.OnExit();

        // 새 상태로 전환 + 진입
        CurrentState = newState;
        CurrentState.OnEnter();
    }

    /// <summary>
    /// EnemyHealth.OnDeath 이벤트의 구독자.
    /// 사망 시점에 호출되어 즉시 DeathState 로 강제 전환.
    /// 현재 어떤 상태였든 (Patrol/Chase/Attack) OnExit → DeathState.OnEnter 진행.
    /// </summary>
    private void HandleDeath()
    {
        ChangeState(DeathState);
    }

    /// <summary>
    /// EnemyHealth.OnDamaged 이벤트의 구독자.
    /// Patrol 중 + 감지 범위 내 피격 시 ChaseState 로 전환.
    /// "뒤에서 맞으면 돌아본다" - 시야각 무관, 거리만 체크 (DetectionRange).
    /// </summary>
    private void HandleDamaged()
    {
        // Patrol 중에만 반응 (Chase/Attack 중은 이미 추격/공격, Death 는 사망)
        if (CurrentState != PatrolState) return;

        // 감지 범위 내 피격 시만 추격 (시야각 무관, 거리만)
        if (_target == null) return;

        float distance = Vector3.Distance(transform.position, _target.position);
        if (distance <= _detectionRange)
        {
            ChangeState(ChaseState);
        }
    }

    // ========================================================================
    // === Vision Helpers ===
    // ========================================================================

    /// <summary>
    /// Target 이 시야 안에 있는지 확인.
    /// 조건: (1) Target 존재 (2) DetectionRange 안 (3) DetectionAngle 안 (정면 기준).
    /// 미래: 장애물 Raycast 추가 가능.
    /// </summary>
    public bool CanSeeTarget()
    {
        if (_target == null) return false;

        Vector3 toTarget = _target.position - transform.position;
        float distance = toTarget.magnitude;

        // 1. 거리 안
        if (distance > _detectionRange) return false;

        // 2. 시야각 안 (수평 평면)
        toTarget.y = 0;
        Vector3 forward = transform.forward;
        forward.y = 0;

        if (toTarget.sqrMagnitude < 0.01f) return true;  // 너무 가까움 (각도 계산 불가) → 발견

        float angle = Vector3.Angle(forward, toTarget);
        return angle < _detectionAngle / 2f;
    }

    // ========================================================================
    // === Editor Visualization ===
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

        // 공격 거리 시각화 (빨간 원)
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, _attackRange);

        // 시야 감지 거리 시각화 (노란 원)
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, _detectionRange);

        // 추격 포기 거리 시각화 (주황 원)
        Gizmos.color = new Color(1f, 0.5f, 0f);
        Gizmos.DrawWireSphere(transform.position, _giveUpRange);

        // 시야각 시각화 (정면 + 좌우 각도 선)
        Gizmos.color = Color.cyan;
        Vector3 forward = transform.forward * _detectionRange;
        Quaternion leftRot = Quaternion.AngleAxis(-_detectionAngle / 2f, Vector3.up);
        Quaternion rightRot = Quaternion.AngleAxis(_detectionAngle / 2f, Vector3.up);
        Gizmos.DrawRay(transform.position, leftRot * forward);
        Gizmos.DrawRay(transform.position, rightRot * forward);
    }
}