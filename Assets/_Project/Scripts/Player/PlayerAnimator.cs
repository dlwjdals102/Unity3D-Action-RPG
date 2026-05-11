using UnityEngine;

/// <summary>
/// 플레이어의 애니메이터 파라미터를 제어하는 컴포넌트.
/// PlayerMovement, PlayerCombat 등이 이 컴포넌트의 메서드를 호출하여
/// 애니메이션을 재생한다. 애니메이터 디테일을 외부로부터 캡슐화한다.
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

    // === Animation Settings ===
    [Header("Animation")]
    [SerializeField] private float _moveSpeedDamping = 0.1f;

    private void Awake()
    {
        _animator = GetComponentInChildren<Animator>();

        if (_animator == null)
        {
            Debug.LogError("[PlayerAnimator] Animator not found in children!");
        }
    }

    // === Public API ===

    /// <summary>
    /// 이동 속도 파라미터 설정. 0=Idle, 0.5=Walk, 1=Run.
    /// 댐핑이 적용되어 부드럽게 전환된다.
    /// </summary>
    public void SetMoveSpeed(float normalizedSpeed)
    {
        _animator.SetFloat(MoveSpeedHash, normalizedSpeed, _moveSpeedDamping, Time.deltaTime);
    }

    /// <summary>
    /// 지면 상태 설정. Jump/Fall/Land 트랜지션 조건으로 사용된다.
    /// </summary>
    public void SetGrounded(bool isGrounded)
    {
        _animator.SetBool(IsGroundedHash, isGrounded);
    }

    /// <summary>
    /// 회피 애니메이션 재생.
    /// </summary>
    public void PlayDodge()
    {
        _animator.SetTrigger(DodgeTriggerHash);
    }

    /// <summary>
    /// 점프 애니메이션 재생.
    /// </summary>
    public void PlayJump()
    {
        _animator.SetTrigger(JumpTriggerHash);
    }
}