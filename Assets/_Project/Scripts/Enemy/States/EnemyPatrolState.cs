using UnityEngine;

/// <summary>
/// 적의 순찰 상태.
/// 지정된 Waypoints 를 순서대로 방문하며 이동한다.
/// Waypoints 가 없으면 Idle 상태로 머무름 (안전망).
/// Animator MoveSpeed 는 실제 이동 속도 기반 (시각 정확성).
/// 시야 안에 Target 이 들어오면 EnemyChaseState 로 전환.
/// </summary>
public class EnemyPatrolState : EnemyStateBase
{
    // 현재 향하는 Waypoint 의 인덱스
    private int _currentIndex;

    public EnemyPatrolState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // Animator 를 Locomotion 으로 명시적 전환
        _stateMachine.Animator.PlayLocomotion();

        // 현재 Waypoint 로 이동 시작
        MoveToCurrentWaypoint();
    }

    public override void OnUpdate()
    {
        // 1. 시야 감지: Target 이 시야 안 → EnemyChaseState 전환
        if (_stateMachine.CanSeeTarget())
        {
            _stateMachine.ChangeState(_stateMachine.ChaseState);
            return;
        }

        // 2. Waypoints 가 없으면 Idle 상태 (안전망)
        Transform[] points = _stateMachine.PatrolPoints;
        if (points == null || points.Length == 0)
        {
            _stateMachine.Movement.StopMoving();
            _stateMachine.Animator.SetMoveSpeed(0f);
            return;
        }

        // 3. 현재 Waypoint 도달 시 다음 Waypoint 선택
        if (_stateMachine.Movement.HasReachedDestination())
        {
            _currentIndex = (_currentIndex + 1) % points.Length;
            MoveToCurrentWaypoint();
        }

        // 4. Animator MoveSpeed 를 실제 속도 기반으로 갱신
        UpdateAnimatorMoveSpeed();
    }

    /// <summary>
    /// 현재 인덱스의 Waypoint 로 이동 시작.
    /// Waypoint 가 null 이면 건너뛰고 다음 인덱스로 진행.
    /// </summary>
    private void MoveToCurrentWaypoint()
    {
        Transform[] points = _stateMachine.PatrolPoints;
        if (points == null || points.Length == 0) return;

        // null Waypoint 건너뛰기 (Inspector 슬롯 일부 None 인 경우)
        int safeguard = 0;
        while (points[_currentIndex] == null && safeguard < points.Length)
        {
            _currentIndex = (_currentIndex + 1) % points.Length;
            safeguard++;
        }

        if (points[_currentIndex] == null) return;  // 모두 null 인 경우

        _stateMachine.Movement.MoveTo(
            points[_currentIndex].position,
            _stateMachine.Movement.WalkSpeed
        );
    }

    /// <summary>
    /// NavMeshAgent 의 실제 속도를 WalkSpeed 로 정규화하여 Animator 에 전달.
    /// 실제 속도 0 → MoveSpeed 0 (Idle), 실제 속도 WalkSpeed → MoveSpeed 0.5 (Walk).
    /// </summary>
    private void UpdateAnimatorMoveSpeed()
    {
        float actualSpeed = _stateMachine.Movement.CurrentSpeed;
        // WalkSpeed 에 도달했을 때 Blend Tree 의 Walk 위치 (0.5) 가 되도록 매핑
        float normalizedSpeed = Mathf.Clamp01(actualSpeed / _stateMachine.Movement.WalkSpeed) * 0.5f;
        _stateMachine.Animator.SetMoveSpeed(normalizedSpeed);
    }
}