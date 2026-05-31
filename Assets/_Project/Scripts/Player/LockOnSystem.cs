using UnityEngine;

/// <summary>
/// 플레이어 락온(Lock-on) 시스템.
/// Tab(LockOnRequested) 토글로 락온을 켜고 끈다.
/// 켤 때: 락온 거리 내 + 화면 안에 있는 적 중 "화면 중앙(조준점)에 가장 가까운" 적을 타겟으로.
/// (가장 가까운 거리 대신 화면 중앙 기준 → 플레이어가 정면으로 보는 적이 잡힘, 뒤쪽 적 배제)
/// 
/// CurrentTarget 을 노출해 카메라/플레이어 회전/UI 가 참조한다.
/// 타겟이 죽거나 해제 거리(락온 거리 × 여유)를 넘으면 자동 해제.
/// </summary>
public class LockOnSystem : MonoBehaviour
{
    [Header("Detection")]
    [Tooltip("락온 가능 최대 거리(m)")]
    [SerializeField] private float _lockOnRange = 15f;

    [Tooltip("락온 대상 적 Layer")]
    [SerializeField] private LayerMask _enemyLayer;

    [Tooltip("락온 해제 거리 배율 (이 거리 × lockOnRange 넘으면 자동 해제)")]
    [SerializeField] private float _releaseRangeMultiplier = 1.5f;

    private PlayerController _controller;
    private Camera _camera;

    private Transform _currentTarget;
    private EnemyHealth _currentTargetHealth;

    /// <summary>현재 락온 대상 (없으면 null). 카메라/회전/UI 가 참조.</summary>
    public Transform CurrentTarget => _currentTarget;

    /// <summary>락온 중인지.</summary>
    public bool IsLockedOn => _currentTarget != null;

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _camera = Camera.main;
    }

    private void Update()
    {
        if (_controller == null) return;

        // Tab 토글 요청 처리
        if (_controller.LockOnRequested)
        {
            Toggle();
        }

        // 락온 중일 때만 타겟 전환 + 유효성 검사
        if (IsLockedOn)
        {
            // 좌/우 타겟 전환 (Q/E)
            if (_controller.SwitchTargetLeftRequested)
            {
                SwitchTarget(toLeft: true);
            }
            else if (_controller.SwitchTargetRightRequested)
            {
                SwitchTarget(toLeft: false);
            }

            ValidateCurrentTarget();
        }
    }

    /// <summary>락온 토글: 켜져 있으면 해제, 아니면 타겟 탐색.</summary>
    private void Toggle()
    {
        if (IsLockedOn)
        {
            ClearTarget();
        }
        else
        {
            Transform target = FindBestTarget();
            if (target != null) SetTarget(target);
        }
    }

    /// <summary>
    /// 락온 거리 내 + 화면 안의 적 중, 화면 중앙(0.5,0.5)에 가장 가까운 적을 찾는다.
    /// </summary>
    private Transform FindBestTarget()
    {
        if (_camera == null) return null;

        Collider[] candidates = Physics.OverlapSphere(transform.position, _lockOnRange, _enemyLayer);

        Transform best = null;
        float bestScreenDist = float.MaxValue;
        Vector2 screenCenter = new Vector2(0.5f, 0.5f);

        foreach (var col in candidates)
        {
            // 죽은 적 제외
            var health = col.GetComponentInParent<EnemyHealth>();
            if (health == null || health.IsDead) continue;

            Transform targetRoot = health.transform;

            // 화면 좌표 변환
            Vector3 viewport = _camera.WorldToViewportPoint(targetRoot.position);

            // 카메라 뒤(z<=0)거나 화면 밖이면 제외
            if (viewport.z <= 0f) continue;
            if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) continue;

            // 화면 중앙과의 거리 (작을수록 조준점에 가까움)
            float screenDist = Vector2.Distance(new Vector2(viewport.x, viewport.y), screenCenter);

            if (screenDist < bestScreenDist)
            {
                bestScreenDist = screenDist;
                best = targetRoot;
            }
        }

        return best;
    }

    /// <summary>
    /// 현재 타겟 기준, 화면상 왼쪽(toLeft=true) 또는 오른쪽의 다음 적으로 전환한다.
    /// 후보가 없으면 현재 타겟 유지.
    /// (FindBestTarget 과 같은 "화면 안 후보 수집" 을 쓰되, 선택 기준이 좌/우 방향)
    /// </summary>
    private void SwitchTarget(bool toLeft)
    {
        if (_camera == null || _currentTarget == null) return;

        // 현재 타겟의 화면 X 좌표
        Vector3 currentVp = _camera.WorldToViewportPoint(_currentTarget.position);
        float currentX = currentVp.x;

        Collider[] candidates = Physics.OverlapSphere(transform.position, _lockOnRange, _enemyLayer);

        Transform best = null;
        float bestXDist = float.MaxValue;

        foreach (var col in candidates)
        {
            var health = col.GetComponentInParent<EnemyHealth>();
            if (health == null || health.IsDead) continue;

            Transform targetRoot = health.transform;
            if (targetRoot == _currentTarget) continue;  // 자기 자신 제외

            Vector3 viewport = _camera.WorldToViewportPoint(targetRoot.position);

            // 카메라 뒤거나 화면 밖이면 제외
            if (viewport.z <= 0f) continue;
            if (viewport.x < 0f || viewport.x > 1f || viewport.y < 0f || viewport.y > 1f) continue;

            // 입력 방향에 있는 적만: 왼쪽 전환이면 현재보다 X 작은 적, 오른쪽이면 X 큰 적
            float xDiff = viewport.x - currentX;
            if (toLeft && xDiff >= 0f) continue;    // 왼쪽인데 오른쪽/같은 위치 → 제외
            if (!toLeft && xDiff <= 0f) continue;   // 오른쪽인데 왼쪽/같은 위치 → 제외

            // 방향 내에서 X 거리가 가장 가까운 적 (현재 타겟에 인접한 적부터)
            float xDist = Mathf.Abs(xDiff);
            if (xDist < bestXDist)
            {
                bestXDist = xDist;
                best = targetRoot;
            }
        }

        if (best != null) SetTarget(best);
    }

    /// <summary>락온 중 타겟이 죽었거나 해제 거리를 넘으면 자동 해제.</summary>
    private void ValidateCurrentTarget()
    {
        if (_currentTargetHealth == null || _currentTargetHealth.IsDead)
        {
            ClearTarget();
            return;
        }

        float dist = Vector3.Distance(transform.position, _currentTarget.position);
        if (dist > _lockOnRange * _releaseRangeMultiplier)
        {
            ClearTarget();
        }
    }

    private void SetTarget(Transform target)
    {
        _currentTarget = target;
        _currentTargetHealth = target.GetComponent<EnemyHealth>();
    }

    private void ClearTarget()
    {
        _currentTarget = null;
        _currentTargetHealth = null;
    }
}