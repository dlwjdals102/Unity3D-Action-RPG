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

    [Header("Patrol")]
    [Tooltip("Waypoint 도달 판정 거리. 공격 거리(stoppingDistance)와 별개. " +
             "stoppingDistance 를 재사용하면 원거리 적(공격 거리 큼)이 Waypoint 사이에 갇히므로 분리.")]
    [SerializeField] private float _waypointArriveDistance = 0.5f;

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
    /// NavMeshAgent 안전 순간이동 (리스폰 등). transform 직접 이동 대신 Warp.
    /// </summary>
    public void Warp(Vector3 position)
    {
        if (_agent != null)
        {
            _agent.Warp(position);
        }
        else
        {
            transform.position = position;
        }
    }

    /// <summary>
    /// NavMeshAgent 를 정상 이동 가능 상태로 복구 (리스폰 시).
    /// 전투 중 StopMoving(isStopped) 이나 BeginCharge(updatePosition=false) 상태가
    /// 남아있으면 부활 후 움직이지 못하므로, 모든 Agent 플래그를 기본값으로 되돌린다.
    /// </summary>
    public void ResetAgent()
    {
        if (_agent == null) return;

        _agent.updatePosition = true;   // BeginCharge 에서 false 됐을 수 있음
        _agent.updateRotation = true;
        _agent.isStopped = false;       // StopMoving/BeginCharge 에서 true 됐을 수 있음
        _agent.ResetPath();             // 이전 경로 제거
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

    // ========================================================================
    // === Charge (돌진) - transform 직접 이동 ===
    // NavMeshAgent 의 경로 이동이 아닌 직선 돌진. 돌진 중 NavMeshAgent 는
    // 자동 이동을 멈추되(updatePosition off), 위치는 transform 과 동기화한다.
    // ========================================================================

    /// <summary>
    /// 돌진 시작. NavMeshAgent 의 자동 위치 갱신을 끄고 직접 이동 모드로 전환.
    /// </summary>
    public void BeginCharge()
    {
        _agent.isStopped = true;
        _agent.ResetPath();
        // NavMeshAgent 가 transform 을 덮어쓰지 않도록 (직접 이동 위해)
        _agent.updatePosition = false;
        _agent.updateRotation = false;
    }

    /// <summary>
    /// 직선 돌진 이동 (고정 방향). NavMesh 경계(벽)에 막히면 그 직전에서 멈추고 false 반환.
    /// NavMesh 위 위치 유지를 위해 NavMeshAgent.nextPosition 도 동기화.
    /// </summary>
    /// <returns>정상 이동 = true, 벽(NavMesh 경계)에 막힘 = false</returns>
    public bool ChargeMove(Vector3 direction, float speed)
    {
        Vector3 nextPos = transform.position + direction * (speed * Time.deltaTime);

        // 다음 위치까지 NavMesh 경계(벽)를 가로지르면 막힘 → 경계 직전까지만 이동
        if (NavMesh.Raycast(transform.position, nextPos, out NavMeshHit hit, NavMesh.AllAreas))
        {
            transform.position = hit.position;
            _agent.nextPosition = transform.position;
            return false;
        }

        transform.position = nextPos;
        _agent.nextPosition = transform.position;
        return true;
    }

    /// <summary>
    /// 돌진 종료. NavMeshAgent 자동 갱신 복원 + 현재 위치를 NavMesh 에 맞춤.
    /// </summary>
    public void EndCharge()
    {
        // 현재 transform 위치를 NavMesh 상으로 워프 (어긋남 보정)
        _agent.Warp(transform.position);
        _agent.updatePosition = true;
        _agent.updateRotation = true;
        _agent.isStopped = true;  // 경직 동안 정지 (ChargeState 가 이후 제어)
    }

    /// <summary>
    /// NavMeshAgent 의 정지 거리를 설정한다.
    /// 상태별로 다른 의미:
    /// - PatrolState: 0 (Waypoint 에 가깝게 도달해야 다음으로 진행)
    /// - ChaseState/AttackState: AttackRange (공격 거리에서 멈춤)
    /// 
    /// 같은 stoppingDistance 가 "Waypoint 도달" 과 "공격 거리" 라는
    /// 상반된 요구를 가지므로, 각 상태가 OnEnter 에서 자기 값을 설정한다.
    /// </summary>
    public void SetStoppingDistance(float distance)
    {
        _agent.stoppingDistance = distance;
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

        // 공격 거리(stoppingDistance)가 아닌 Waypoint 전용 도달 거리 사용.
        // (원거리 적의 큰 stoppingDistance 로 인한 "Waypoint 사이 갇힘" 방지)
        return _agent.remainingDistance <= _waypointArriveDistance;
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