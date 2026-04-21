using System;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// 적 컨트롤러. NavMeshAgent 이동과 IDamageable을 구현합니다.
/// EnemyAI가 이 클래스의 메서드를 호출하여 적을 제어합니다.
/// </summary>
[RequireComponent(typeof(NavMeshAgent))]
[RequireComponent(typeof(Animator))]
public class EnemyController : MonoBehaviour, IDamageable
{
    // ════════════════════════════════════════════════════
    //  설정
    // ════════════════════════════════════════════════════

    [Header("Data")]
    [SerializeField] private EnemyData _data;

    [Header("Hit Feedback")]
    [SerializeField] private float _flashDuration = 0.15f;

    // ════════════════════════════════════════════════════
    //  IDamageable 구현
    // ════════════════════════════════════════════════════

    public float CurrentHp { get; private set; }
    public float MaxHp => _data != null ? _data.maxHp : 100f;
    public bool IsAlive => CurrentHp > 0f;

    // ════════════════════════════════════════════════════
    //  프로퍼티
    // ════════════════════════════════════════════════════

    public EnemyData Data => _data;
    public NavMeshAgent Agent { get; private set; }
    public Animator Animator { get; private set; }
    public Transform PlayerTransform { get; private set; }

    /// <summary>플레이어와의 거리</summary>
    public float DistanceToPlayer
    {
        get
        {
            if (PlayerTransform == null) return float.MaxValue;
            return Vector3.Distance(transform.position, PlayerTransform.position);
        }
    }

    // ── 이벤트 ──
    public event Action<DamageData> OnDamaged;
    public event Action OnDeath;

    // ── 내부 ──
    private Renderer _renderer;
    private Color _originalColor;
    private Coroutine _flashCoroutine;
    private Coroutine _knockbackCoroutine;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        Agent = GetComponent<NavMeshAgent>();
        Animator = GetComponent<Animator>();
        _renderer = GetComponentInChildren<Renderer>();

        if (_renderer != null)
            _originalColor = _renderer.material.color;

        if (_data != null)
        {
            CurrentHp = _data.maxHp;
            Agent.speed = _data.moveSpeed;
            Agent.stoppingDistance = _data.stopChaseRange;
        }
    }

    private void Start()
    {
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag(Define.Tag.Player);
        if (player != null)
            PlayerTransform = player.transform;
    }

    // ════════════════════════════════════════════════════
    //  IDamageable
    // ════════════════════════════════════════════════════

    public float TakeDamage(DamageData data)
    {
        if (!IsAlive) return 0f;

        float actualDamage = Mathf.Min(data.Amount, CurrentHp);
        CurrentHp -= actualDamage;

        Debug.Log(
            $"[{_data?.enemyName ?? name}] 피격! 데미지: {actualDamage:F0} | " +
            $"HP: {CurrentHp:F0}/{MaxHp:F0}"
        );

        // 넉백 (코루틴으로 부드럽게 이동)
        if (data.KnockbackForce > 0f && Agent.enabled)
        {
            if (_knockbackCoroutine != null)
                StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = StartCoroutine(KnockbackRoutine(data.KnockbackDirection, data.KnockbackForce));
        }

        // 피격 플래시
        if (_renderer != null)
        {
            if (_flashCoroutine != null) StopCoroutine(_flashCoroutine);
            _flashCoroutine = StartCoroutine(FlashRoutine());
        }

        // 이벤트 발행
        OnDamaged?.Invoke(data);

        // 사망 체크
        if (!IsAlive)
        {
            OnDeath?.Invoke();

            if (GameManager.HasInstance)
                GameManager.Instance.NotifyEnemyKilled(gameObject);
        }

        return actualDamage;
    }

    // ════════════════════════════════════════════════════
    //  이동 제어 (EnemyAI에서 호출)
    // ════════════════════════════════════════════════════

    /// <summary>목표 위치로 이동합니다.</summary>
    public void MoveTo(Vector3 position)
    {
        if (!Agent.enabled) return;
        Agent.isStopped = false;
        Agent.SetDestination(position);
    }

    /// <summary>이동을 멈춥니다.</summary>
    public void StopMovement()
    {
        if (!Agent.enabled) return;
        Agent.isStopped = true;
        Agent.velocity = Vector3.zero;
    }

    /// <summary>대상을 바라봅니다.</summary>
    public void LookAtTarget(Vector3 targetPos)
    {
        Vector3 dir = (targetPos - transform.position).normalized;
        dir.y = 0f;
        if (dir != Vector3.zero)
            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                Quaternion.LookRotation(dir),
                10f * Time.deltaTime
            );
    }

    /// <summary>NavMeshAgent를 비활성화합니다 (사망 시).</summary>
    public void DisableAgent()
    {
        if (Agent.enabled)
        {
            Agent.isStopped = true;
            Agent.enabled = false;
        }
    }

    /// <summary>적을 초기 상태로 리셋합니다 (리스폰 시).</summary>
    public void ResetEnemy()
    {
        CurrentHp = MaxHp;

        if (_renderer != null)
            _renderer.material.color = _originalColor;

        if (_knockbackCoroutine != null)
        {
            StopCoroutine(_knockbackCoroutine);
            _knockbackCoroutine = null;
        }

        if (!Agent.enabled)
            Agent.enabled = true;

        Agent.isStopped = false;
    }

    /// <summary>Animator Speed 파라미터를 업데이트합니다.</summary>
    public void UpdateAnimator()
    {
        float speed = Agent.enabled ? Agent.velocity.magnitude / Agent.speed : 0f;
        Animator.SetFloat(Define.AnimParam.Speed, speed, 0.1f, Time.deltaTime);
    }

    // ════════════════════════════════════════════════════
    //  시각 피드백
    // ════════════════════════════════════════════════════

    private System.Collections.IEnumerator FlashRoutine()
    {
        if (_renderer != null) _renderer.material.color = Color.red;
        yield return new WaitForSeconds(_flashDuration);
        if (_renderer != null)
            _renderer.material.color = IsAlive ? _originalColor : Color.gray;
        _flashCoroutine = null;
    }

    /// <summary>
    /// NavMeshAgent를 일시 정지하고 부드럽게 넉백 이동시킵니다.
    /// velocity를 직접 설정하는 방식과 달리 누적되지 않으며,
    /// 종료 시 NavMeshAgent를 정상 복귀시킵니다.
    /// </summary>
    private System.Collections.IEnumerator KnockbackRoutine(Vector3 direction, float force)
    {
        const float duration = 0.2f;

        // NavMeshAgent 일시 정지
        bool wasStopped = Agent.isStopped;
        Agent.isStopped = true;
        Agent.velocity = Vector3.zero;
        Agent.ResetPath();

        // Y축 무시 (수평 넉백만)
        direction.y = 0f;
        direction = direction.normalized;

        float elapsed = 0f;
        while (elapsed < duration)
        {
            // 시간에 따라 강도 감소 (강 → 약)
            float t = elapsed / duration;
            float currentForce = force * (1f - t);

            // NavMeshAgent.Move는 NavMesh 위에서만 이동 (벽을 뚫지 않음)
            Vector3 movement = direction * currentForce * Time.deltaTime;
            Agent.Move(movement);

            elapsed += Time.deltaTime;
            yield return null;
        }

        // NavMeshAgent 재개
        if (Agent.enabled && IsAlive)
            Agent.isStopped = wasStopped;

        _knockbackCoroutine = null;
    }
}