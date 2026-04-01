using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 상태 기반 적 AI.
/// Idle → Patrol → Chase → Attack → (Hit) → (Die)
/// 
/// [면접 포인트]
/// - 단순한 enum FSM이지만 각 상태의 진입/실행/이탈이 명확
/// - 감지 범위와 추적 해제 범위를 분리하여 히스테리시스 적용
///   (detectRange < loseRange → 경계선에서 왔다갔다하는 문제 방지)
/// - 공격 쿨다운으로 연속 공격 방지
/// </summary>
[RequireComponent(typeof(EnemyController))]
public class EnemyAI : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  AI 상태
    // ════════════════════════════════════════════════════

    private enum AIState
    {
        Idle,
        Patrol,
        Chase,
        Attack,
        Hit,
        Die
    }

    // ════════════════════════════════════════════════════
    //  참조
    // ════════════════════════════════════════════════════

    private EnemyController _controller;
    private EnemyData _data;

    // ════════════════════════════════════════════════════
    //  상태 변수
    // ════════════════════════════════════════════════════

    private AIState _currentState = AIState.Idle;
    private float _stateTimer = 0f;

    // Patrol
    private Vector3 _spawnPosition;
    private Vector3 _patrolTarget;
    private bool _hasPatrolTarget = false;

    // Attack
    private float _lastAttackTime = -999f;
    private bool _isAttacking = false;

    // Hit
    private float _hitStunDuration = 0.5f;

    // HitBox (적의 공격 판정)
    [Header("Attack")]
    [SerializeField] private HitBox _attackHitBox;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        _controller = GetComponent<EnemyController>();
    }

    private void Start()
    {
        _data = _controller.Data;
        _spawnPosition = transform.position;

        // 이벤트 구독
        _controller.OnDamaged += OnDamaged;
        _controller.OnDeath += OnDeath;

        TransitionTo(AIState.Idle);
    }

    private void OnDestroy()
    {
        if (_controller != null)
        {
            _controller.OnDamaged -= OnDamaged;
            _controller.OnDeath -= OnDeath;
        }
    }

    // ════════════════════════════════════════════════════
    //  업데이트
    // ════════════════════════════════════════════════════

    private void Update()
    {
        _stateTimer += Time.deltaTime;
        _controller.UpdateAnimator();

        switch (_currentState)
        {
            case AIState.Idle: UpdateIdle(); break;
            case AIState.Patrol: UpdatePatrol(); break;
            case AIState.Chase: UpdateChase(); break;
            case AIState.Attack: UpdateAttack(); break;
            case AIState.Hit: UpdateHit(); break;
            case AIState.Die: break;
        }
    }

    // ════════════════════════════════════════════════════
    //  상태 전환
    // ════════════════════════════════════════════════════

    private void TransitionTo(AIState newState)
    {
        // 이전 상태 Exit
        ExitState(_currentState);

        _currentState = newState;
        _stateTimer = 0f;

        // 새 상태 Enter
        EnterState(newState);
    }

    private void EnterState(AIState state)
    {
        switch (state)
        {
            case AIState.Idle:
                _controller.StopMovement();
                break;

            case AIState.Patrol:
                PickPatrolTarget();
                break;

            case AIState.Chase:
                break;

            case AIState.Attack:
                _controller.StopMovement();
                _controller.Agent.ResetPath();
                _isAttacking = true;
                PerformAttack();
                break;

            case AIState.Hit:
                _controller.StopMovement();
                _controller.Agent.ResetPath();
                CancelInvoke();
                _attackHitBox?.DisableHitBox();
                _controller.Animator.SetTrigger(Define.AnimParam.Hit);
                break;

            case AIState.Die:
                _controller.StopMovement();
                CancelInvoke();
                _attackHitBox?.DisableHitBox();
                _controller.DisableAgent();
                _controller.Animator.SetTrigger(Define.AnimParam.Die);
                break;
        }
    }

    private void ExitState(AIState state)
    {
        switch (state)
        {
            case AIState.Attack:
                _isAttacking = false;
                CancelInvoke(nameof(EnableAttackHitBox));
                CancelInvoke(nameof(DisableAttackHitBox));
                _attackHitBox?.DisableHitBox();
                break;
        }
    }

    // ════════════════════════════════════════════════════
    //  Idle 상태
    // ════════════════════════════════════════════════════

    private void UpdateIdle()
    {
        // 플레이어 감지 → Chase
        if (IsPlayerInRange(_data.detectRange))
        {
            TransitionTo(AIState.Chase);
            return;
        }

        // 일정 시간 후 Patrol
        if (_stateTimer >= _data.patrolWaitTime)
        {
            TransitionTo(AIState.Patrol);
        }
    }

    // ════════════════════════════════════════════════════
    //  Patrol 상태 (순찰)
    // ════════════════════════════════════════════════════

    private void UpdatePatrol()
    {
        // 플레이어 감지 → Chase
        if (IsPlayerInRange(_data.detectRange))
        {
            TransitionTo(AIState.Chase);
            return;
        }

        // 순찰 목표에 도달했는지 확인
        if (_hasPatrolTarget)
        {
            float dist = Vector3.Distance(transform.position, _patrolTarget);
            if (dist < 1.5f)
            {
                _hasPatrolTarget = false;
                TransitionTo(AIState.Idle);
            }
        }
    }

    private void PickPatrolTarget()
    {
        // 스폰 위치 주변 랜덤 지점
        Vector2 randomCircle = Random.insideUnitCircle * _data.patrolRadius;
        Vector3 randomPoint = _spawnPosition + new Vector3(randomCircle.x, 0f, randomCircle.y);

        // NavMesh 위의 유효한 위치 찾기
        if (NavMesh.SamplePosition(randomPoint, out NavMeshHit hit, _data.patrolRadius, NavMesh.AllAreas))
        {
            _patrolTarget = hit.position;
            _hasPatrolTarget = true;
            _controller.MoveTo(_patrolTarget);
        }
        else
        {
            // 유효 위치 못 찾으면 Idle로
            TransitionTo(AIState.Idle);
        }
    }

    // ════════════════════════════════════════════════════
    //  Chase 상태 (추적)
    // ════════════════════════════════════════════════════

    private void UpdateChase()
    {
        // 플레이어 추적 해제 범위 밖 → Idle
        if (!IsPlayerInRange(_data.loseRange))
        {
            TransitionTo(AIState.Idle);
            return;
        }

        // 공격 범위 안 → Attack
        if (IsPlayerInRange(_data.attackRange))
        {
            if (Time.time - _lastAttackTime >= _data.attackCooldown)
            {
                TransitionTo(AIState.Attack);
                return;
            }
        }

        // 플레이어 추적
        if (_controller.PlayerTransform != null)
        {
            _controller.MoveTo(_controller.PlayerTransform.position);
            _controller.LookAtTarget(_controller.PlayerTransform.position);
        }
    }

    // ════════════════════════════════════════════════════
    //  Attack 상태
    // ════════════════════════════════════════════════════

    private void UpdateAttack()
    {
        // 공격 애니메이션이 끝나면 복귀
        if (_stateTimer >= 1.2f)
        {
            _isAttacking = false;
            _lastAttackTime = Time.time;

            if (IsPlayerInRange(_data.detectRange))
                TransitionTo(AIState.Chase);
            else
                TransitionTo(AIState.Idle);
        }
    }

    private void PerformAttack()
    {
        _controller.Animator.SetTrigger(Define.AnimParam.Attack);

        // HitBox 활성화 (잠시 후 — 공격 모션 타이밍에 맞춤)
        Invoke(nameof(EnableAttackHitBox), 0.4f);
        Invoke(nameof(DisableAttackHitBox), 0.7f);
    }

    private void EnableAttackHitBox()
    {
        _attackHitBox?.EnableHitBox();
    }

    private void DisableAttackHitBox()
    {
        _attackHitBox?.DisableHitBox();
    }

    // ════════════════════════════════════════════════════
    //  Hit 상태 (피격)
    // ════════════════════════════════════════════════════

    private void UpdateHit()
    {
        if (_stateTimer >= _hitStunDuration)
        {
            if (IsPlayerInRange(_data.detectRange))
                TransitionTo(AIState.Chase);
            else
                TransitionTo(AIState.Idle);
        }
    }

    // ════════════════════════════════════════════════════
    //  이벤트 핸들러
    // ════════════════════════════════════════════════════

    private void OnDamaged(DamageData data)
    {
        if (!_controller.IsAlive) return;
        if (_currentState == AIState.Die) return;
        if (_currentState == AIState.Hit) return;

        TransitionTo(AIState.Hit);
    }

    private void OnDeath()
    {
        TransitionTo(AIState.Die);

        // 일정 시간 후 파괴 (또는 리스폰)
        Destroy(gameObject, 5f);
    }

    // ════════════════════════════════════════════════════
    //  유틸리티
    // ════════════════════════════════════════════════════

    private bool IsPlayerInRange(float range)
    {
        return _controller.DistanceToPlayer <= range;
    }

    // ════════════════════════════════════════════════════
    //  디버그 시각화
    // ════════════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        float detectRange = _data != null ? _data.detectRange : 10f;
        float attackRange = _data != null ? _data.attackRange : 2f;
        float loseRange = _data != null ? _data.loseRange : 15f;
        float patrolRadius = _data != null ? _data.patrolRadius : 5f;

        // 감지 범위 (노란색)
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f);
        Gizmos.DrawWireSphere(transform.position, detectRange);

        // 추적 해제 범위 (빨간색)
        Gizmos.color = new Color(1f, 0f, 0f, 0.1f);
        Gizmos.DrawWireSphere(transform.position, loseRange);

        // 공격 범위 (빨간색)
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // 순찰 범위 (초록색)
        Vector3 spawnPos = Application.isPlaying ? _spawnPosition : transform.position;
        Gizmos.color = new Color(0f, 1f, 0f, 0.15f);
        Gizmos.DrawWireSphere(spawnPos, patrolRadius);
    }

    private void OnGUI()
    {
#if UNITY_EDITOR
        Vector3 screenPos = Camera.main.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);
        if (screenPos.z > 0)
        {
            GUI.Label(
                new Rect(screenPos.x - 60, Screen.height - screenPos.y, 120, 20),
                $"{_data?.enemyName}: {_currentState}"
            );
        }
#endif
    }
}