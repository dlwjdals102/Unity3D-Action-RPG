using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적의 이동을 NavMeshAgent 로 처리하는 컴포넌트.
/// 상태 (PatrolState, ChaseState, AttackState 등) 가 호출할 Public API 제공.
/// 단일 책임: 적의 이동 + 회전.
/// 
/// 수치 데이터 (WalkSpeed, ChaseSpeed) 는 EnemyConfig 에서 읽는다.
/// stoppingDistance 는 Config.AttackRange 와 동기화 (단일 출처).
/// _rotationSpeed 는 시각 느낌이라 컴포넌트가 보유.
/// 
/// PlayerMovement 와 본질이 다름:
/// - 입력 없음 (AI 결정)
/// - 점프/중력 없음 (NavMesh 평면)
/// - 회전: NavMeshAgent 자동 + LookAt (부드러운) + SetRotationImmediate (즉시)
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    [Header("Config")]
    [Tooltip("적의 수치 데이터 (속도, 공격 거리 등)")]
    [SerializeField] private EnemyConfig _config;

    [Header("Rotation")]
    [Tooltip("LookAt 호출 시 회전 속도 (Slerp 보간). 시각 느낌이라 Config 와 분리")]
    [SerializeField] private float _rotationSpeed = 8f;

    private NavMeshAgent _agent;

    // === Public Properties (상태가 접근) ===
    public float WalkSpeed => _config != null ? _config.WalkSpeed : 0f;
    public float ChaseSpeed => _config != null ? _config.ChaseSpeed : 0f;
    public bool IsMoving => _agent.velocity.sqrMagnitude > 0.01f;
    public float CurrentSpeed => _agent.velocity.magnitude;
    public Vector3 Velocity => _agent.velocity;

    private void Awake()
    {
        _agent = GetComponent<NavMeshAgent>();

        if (_config == null)
        {
            Debug.LogError($"[EnemyMovement] EnemyConfig not assigned on {gameObject.name}!");
            return;
        }

        // stoppingDistance 를 Config.AttackRange 와 동기화 (단일 출처).
        // 공격 거리 = 정지 거리. 데이터 중복 회피.
        _agent.stoppingDistance = _config.AttackRange;
    }

    // ========================================================================
    // === Public API (각 상태가 호출) ===
    // ========================================================================

    /// <summary>
    /// 지정된 위치로 이동 시작. NavMeshAgent 가 경로 계산 + 자동 이동.
    /// 호출 시 즉시 이동 가능 상태 (isStopped = false).
    /// </summary>
    public void MoveTo(Vector3 worldPosition, float speed)
    {
        _agent.speed = speed;
        _agent.isStopped = false;
        _agent.SetDestination(worldPosition);
    }

    /// <summary>
    /// 즉시 이동 정지. 경로도 초기화.
    /// AttackState 진입 시 또는 정지 필요 시 호출.
    /// </summary>
    public void StopMoving()
    {
        if (_agent.isStopped) return;

        _agent.isStopped = true;
        _agent.ResetPath();
    }

    /// <summary>
    /// 지정된 위치 향해 부드러운 회전 (Slerp 보간).
    /// 정지 상태에서 플레이어 향하기 등에 사용.
    /// 매 프레임 호출 필요 (Update 또는 상태의 OnUpdate).
    /// </summary>
    public void LookAt(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0;  // 수평 회전만

        if (direction.sqrMagnitude < 0.01f) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _rotationSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// 지정된 위치 향해 즉시 회전 (보간 없음).
    /// AttackState 의 OnEnter 처럼 즉각 반응이 필요한 경우 사용.
    /// </summary>
    public void SetRotationImmediate(Vector3 worldPosition)
    {
        Vector3 direction = worldPosition - transform.position;
        direction.y = 0;  // 수평 회전만

        if (direction.sqrMagnitude < 0.01f) return;

        transform.rotation = Quaternion.LookRotation(direction);
    }

    /// <summary>
    /// 현재 목표 위치에 도달했는지 확인.
    /// ChaseState 가 "공격 거리 도달?" 체크에 활용.
    /// </summary>
    public bool HasReachedDestination()
    {
        if (_agent.pathPending) return false;
        if (!_agent.hasPath) return false;

        return _agent.remainingDistance <= _agent.stoppingDistance;
    }

    /// <summary>
    /// 플레이어와의 거리 (간단한 헬퍼).
    /// 상태가 거리 기반 분기에 활용.
    /// </summary>
    public float DistanceTo(Vector3 worldPosition)
    {
        return Vector3.Distance(transform.position, worldPosition);
    }
}