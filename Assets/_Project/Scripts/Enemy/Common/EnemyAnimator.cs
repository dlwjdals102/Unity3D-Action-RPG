using UnityEngine;

/// <summary>
/// 적의 Animator 제어 컴포넌트.
/// 상태가 호출할 Public API 제공 + Animation Event 처리.
/// PlayerAnimator 의 AnyState 기반 트리거 패턴 일관.
/// 단일 책임: 적 애니메이션 상태 제어.
/// 
/// 부모 Enemy 에 부착되며, 자식 Model 의 Animator 를 GetComponentInChildren 으로 참조.
/// Animation Event 는 Model 자식의 EnemyAnimationEventReceiver 가 라우팅.
/// 
/// Animator Parameters:
/// - MoveSpeed (Float): Locomotion 블렌딩 (0=Idle, 0.5=Walk, 1=Run)
/// - Locomotion (Trigger): Locomotion 상태로 강제 전환
/// - Attack (Trigger): 공격 발동
/// - Death (Trigger): 사망 발동
/// </summary>
public class EnemyAnimator : MonoBehaviour
{
    // === Animator Parameter Hashes (성능 최적화) ===
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int LocomotionTriggerHash = Animator.StringToHash("Locomotion");
    private static readonly int AttackTriggerHash = Animator.StringToHash("Attack");
    private static readonly int DeathTriggerHash = Animator.StringToHash("Death");
    private static readonly int IsStunnedHash = Animator.StringToHash("IsStunned");

    [Header("Animation Settings")]
    [Tooltip("MoveSpeed 댐핑 시간 (부드러운 전환)")]
    [SerializeField] private float _moveSpeedDampTime = 0.1f;

    private Animator _animator;

    // === Animation Event 상태 추적 ===
    private bool _isAttackFinished;
    private bool _isDeathFinished;

    // === Public Properties (상태가 접근) ===
    public bool IsAttackFinished => _isAttackFinished;
    public bool IsDeathFinished => _isDeathFinished;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogError("[EnemyAnimator] Animator not found in children!");
        }
    }

    // ========================================================================
    // === Public API (각 상태가 호출) ===
    // ========================================================================

    /// <summary>
    /// Locomotion 블렌딩을 위한 MoveSpeed 설정.
    /// 0 = Idle, 0.5 = Walk, 1 = Run.
    /// 댐핑 적용으로 부드러운 전환.
    /// </summary>
    public void SetMoveSpeed(float normalizedSpeed)
    {
        if (_animator == null) return;
        _animator.SetFloat(MoveSpeedHash, normalizedSpeed, _moveSpeedDampTime, Time.deltaTime);
    }

    /// <summary>경직(스턴) on/off. 경직 상태 동안 비틀거리는 루프 재생.</summary>
    public void SetStunned(bool isStunned)
    {
        if (_animator != null)
        {
            _animator.SetBool(IsStunnedHash, isStunned);
        }
    }

    /// <summary>
    /// 강제로 Locomotion 상태로 전환.
    /// 이전 상태가 무엇이든 (Attack, Death 등) 즉시 Locomotion 으로 복귀.
    /// IdleState, PatrolState, ChaseState 의 OnEnter 에서 호출.
    /// </summary>
    public void PlayLocomotion()
    {
        if (_animator == null) return;
        ResetAllTriggers();
        _animator.SetTrigger(LocomotionTriggerHash);
    }

    /// <summary>
    /// 대기(Idle) 자세로 전환. 공격 후 쿨다운 등 "멈춰서 대기" 상황에 사용.
    /// 내부적으로 Locomotion 상태 전환 + MoveSpeed 0 (Idle 블렌드).
    /// 
    /// SetMoveSpeed 는 댐핑이 걸려 한 번 호출로 0 에 도달하지 않으므로,
    /// 매 프레임 호출해야 한다 (대기 상태의 OnUpdate 에서).
    /// "이동(Locomotion)" 이 아닌 "대기(Idle)" 라는 의도를 명확히 하는 메서드.
    /// </summary>
    public void PlayIdle()
    {
        if (_animator == null) return;
        // Attack 등 다른 상태에서 Locomotion 으로 전환
        _animator.SetTrigger(LocomotionTriggerHash);
        // Idle 블렌드 (매 프레임 호출 시 댐핑으로 0 수렴)
        _animator.SetFloat(MoveSpeedHash, 0f, _moveSpeedDampTime, Time.deltaTime);
    }

    /// <summary>
    /// 공격 애니메이션 발동. AttackState 의 OnEnter 에서 호출.
    /// 호출 시 _isAttackFinished 플래그 리셋.
    /// </summary>
    public void PlayAttack()
    {
        if (_animator == null) return;
        ResetAllTriggers();
        _isAttackFinished = false;
        _animator.SetTrigger(AttackTriggerHash);
    }

    /// <summary>
    /// 사망 애니메이션 발동. DeathState 의 OnEnter 에서 호출.
    /// 호출 시 _isDeathFinished 플래그 리셋.
    /// </summary>
    public void PlayDeath()
    {
        if (_animator == null) return;
        ResetAllTriggers();
        _isDeathFinished = false;
        _animator.SetTrigger(DeathTriggerHash);
    }

    /// <summary>
    /// 사망 애니메이션 상태 리셋 (리스폰 시). Locomotion 으로 복귀 + 플래그 초기화.
    /// </summary>  
    public void ResetDeathState()
    {
        _isDeathFinished = false;
        PlayLocomotion();  // 사망 자세 → 기본 자세
    }

    // ========================================================================
    // === Animation Event Callbacks (EnemyAnimationEventReceiver 가 라우팅) ===
    // ========================================================================

    /// <summary>
    /// 공격 애니메이션 종료 시 호출.
    /// </summary>
    public void OnAttackAnimationFinished()
    {
        _isAttackFinished = true;
    }

    /// <summary>
    /// 사망 애니메이션 종료 시 호출.
    /// </summary>
    public void OnDeathAnimationFinished()
    {
        _isDeathFinished = true;
    }

    // ========================================================================
    // === Internal Helpers ===
    // ========================================================================

    /// <summary>
    /// 모든 Trigger 를 리셋한다.
    /// 새 트리거 호출 전에 호출하여 이전 트리거가 큐에 쌓이지 않도록 보장.
    /// </summary>
    private void ResetAllTriggers()
    {
        foreach (AnimatorControllerParameter param in _animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                _animator.ResetTrigger(param.nameHash);
            }
        }
    }
}