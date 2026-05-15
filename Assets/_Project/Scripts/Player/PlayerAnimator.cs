using UnityEngine;

/// <summary>
/// 플레이어의 애니메이터 파라미터를 제어하는 컴포넌트.
/// 외부(상태 등)는 목표값만 설정하면 되며, 댐핑/매 프레임 갱신은
/// 이 컴포넌트가 자체적으로 처리한다.
/// 각 Play 메서드는 새 Trigger 발사 전에 모든 Trigger 를 자동 Reset 하여 
/// Trigger 누적 문제를 방지한다.
/// </summary>
public class PlayerAnimator : MonoBehaviour
{
    // === References ===
    private Animator _animator;

    // === Animator Hash IDs (성능 최적화) ===
    private static readonly int MoveSpeedHash = Animator.StringToHash("MoveSpeed");
    private static readonly int IsGroundedHash = Animator.StringToHash("IsGrounded");
    private static readonly int DodgeTriggerHash = Animator.StringToHash("Dodge");
    private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
    private static readonly int FallTriggerHash = Animator.StringToHash("Fall");
    private static readonly int LandTriggerHash = Animator.StringToHash("Land");
    private static readonly int LocomotionTriggerHash = Animator.StringToHash("Locomotion");

    // === Animation Settings ===
    [Header("Animation")]
    [SerializeField] private float _moveSpeedDamping = 0.1f;

    // === Target Values (외부에서 설정, 매 프레임 자동 갱신) ===
    private float _targetMoveSpeed;

    // === Animation Event Flags ===
    private bool _landFinished;
    private bool _dodgeFinished;

    // === Public Properties ===
    public bool IsLandFinished => _landFinished;
    public bool IsDodgeFinished => _dodgeFinished;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogError("[PlayerAnimator] Animator not found in children!");
        }
    }

    private void Update()
    {
        // 매 프레임 댐핑 적용하여 부드러운 전환
        if (_animator != null)
        {
            _animator.SetFloat(MoveSpeedHash, _targetMoveSpeed, _moveSpeedDamping, Time.deltaTime);
        }
    }

    // === Public API ===

    /// <summary>
    /// 이동 속도 목표값 설정. 0=Idle, 0.5=Walk, 1=Run.
    /// 호출자는 1번만 호출하면 되며, 댐핑은 PlayerAnimator 가 자동 처리한다.
    /// </summary>
    public void SetMoveSpeed(float normalizedSpeed)
    {
        _targetMoveSpeed = normalizedSpeed;
    }

    /// <summary>
    /// 지면 상태 설정. Bool 파라미터라 즉시 변경.
    /// </summary>
    public void SetGrounded(bool isGrounded)
    {
        if (_animator != null)
            _animator.SetBool(IsGroundedHash, isGrounded);
    }

    /// <summary>
    /// 회피 애니메이션 재생 (Trigger). DodgeState 의 OnEnter 에서 호출.
    /// 호출 시 _dodgeFinished 플래그를 자동으로 false 로 리셋.
    /// </summary>
    public void PlayDodge()
    {
        _dodgeFinished = false;

        if (_animator == null) return;
        ResetAllTriggers();
        _animator.SetTrigger(DodgeTriggerHash);
    }

    /// <summary>
    /// 점프 애니메이션 재생 (Trigger). JumpState 의 OnEnter 에서 호출.
    /// </summary>
    public void PlayJump()
    {
        if (_animator == null) return;
        ResetAllTriggers();
        _animator.SetTrigger(JumpTriggerHash);
    }

    /// <summary>
    /// 낙하 애니메이션 재생 (Trigger). FallState 의 OnEnter 에서 호출.
    /// </summary>
    public void PlayFall()
    {
        if (_animator == null) return;
        ResetAllTriggers();
        _animator.SetTrigger(FallTriggerHash);
    }

    /// <summary>
    /// 착지 애니메이션 재생 (Trigger). LandState 의 OnEnter 에서 호출.
    /// 호출 시 _landFinished 플래그를 자동으로 false 로 리셋.
    /// </summary>
    public void PlayLand()
    {
        _landFinished = false;

        if (_animator == null) return;
        ResetAllTriggers();
        _animator.SetTrigger(LandTriggerHash);
    }

    /// <summary>
    /// Locomotion 상태로 전환 (Trigger).
    /// IdleState 와 MoveState 의 OnEnter 에서 호출.
    /// </summary>
    public void PlayLocomotion()
    {
        if (_animator == null) return;
        ResetAllTriggers();
        _animator.SetTrigger(LocomotionTriggerHash);
    }

    // === Animation Event Callbacks ===

    /// <summary>
    /// Land 애니메이션 종료 시점에 호출 (PlayerAnimationEventReceiver 가 라우팅).
    /// Mixamo Hard Landing 클립의 끝 부분에 Animation Event 로 설정된다.
    /// </summary>
    public void OnLandAnimationFinished()
    {
        _landFinished = true;
    }

    /// <summary>
    /// Dodge 애니메이션 종료 시점에 호출 (PlayerAnimationEventReceiver 가 라우팅).
    /// Mixamo Roll 클립의 끝 부분에 Animation Event 로 설정된다.
    /// </summary>
    public void OnDodgeAnimationFinished()
    {
        _dodgeFinished = true;
    }

    // === Internal Helpers ===

    /// <summary>
    /// Animator 의 모든 Trigger 파라미터를 자동으로 Reset 한다.
    /// 각 Play 메서드 호출 시 자동 실행되어 Trigger 누적을 방지한다.
    /// 새 Trigger 추가 시 코드 수정 불필요 (자동 순회).
    /// </summary>
    private void ResetAllTriggers()
    {
        foreach (var param in _animator.parameters)
        {
            if (param.type == AnimatorControllerParameterType.Trigger)
            {
                _animator.ResetTrigger(param.nameHash);
            }
        }
    }
}