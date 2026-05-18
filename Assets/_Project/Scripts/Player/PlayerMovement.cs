using UnityEngine;

/// <summary>
/// 플레이어의 이동에 필요한 공통 기능을 제공하는 컴포넌트.
/// 매 프레임 공통 작업 (지면 감지, 중력 누적, 쿨다운 갱신) 을 수행하고,
/// 상태 (IdleState, MoveState 등) 가 호출할 Public API 를 제공한다.
/// Move 호출은 LateUpdate 에서 1번만 수행하여 누적된 속도를 일괄 적용한다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerAnimator))]
public class PlayerMovement : MonoBehaviour
{
    // === Constants ===
    private const float MovementInputThreshold = 0.01f;
    private const float GroundedYVelocity = -2f;

    // === References ===
    private CharacterController _characterController;
    private PlayerController _controller;
    private PlayerAnimator _playerAnimator;
    private Transform _cameraTransform;
    private Transform _groundCheck;

    // === Movement Settings ===
    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _runSpeed = 5.5f;
    [SerializeField] private float _rotationSpeed = 12f;

    // === Gravity & Jump ===
    [Header("Gravity & Jump")]
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _jumpHeight = 1.5f;

    // === Dodge ===
    [Header("Dodge")]
    [SerializeField] private float _dodgeSpeed = 7f;
    [SerializeField] private float _dodgeCooldown = 0.2f;

    // === Stamina Cost ===
    [Header("Stamina Cost")]
    [Tooltip("회피 시 소모되는 스태미나")]
    [SerializeField] private float _dodgeStaminaCost = 25f;

    [Tooltip("달리기 시 초당 소모되는 스태미나")]
    [SerializeField] private float _sprintStaminaCostPerSecond = 10f;

    [Tooltip("새로 달리기 시작에 필요한 최소 스태미나")]
    [SerializeField] private float _minStaminaToStartSprint = 20f;

    // === Ground Check ===
    [Header("Ground Check")]
    [SerializeField] private float _groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask _groundLayer;

    // === Internal State ===
    private Vector3 _verticalVelocity;
    private Vector3 _pendingHorizontalVelocity;
    private bool _isGrounded;
    private float _dodgeTimer;

    // === Public Properties (상태가 접근) ===
    public float WalkSpeed => _walkSpeed;
    public float RunSpeed => _runSpeed;
    public float RotationSpeed => _rotationSpeed;
    public float DodgeSpeed => _dodgeSpeed;
    public float DodgeCooldown => _dodgeCooldown;
    public float DodgeStaminaCost => _dodgeStaminaCost;
    public float SprintStaminaCostPerSecond => _sprintStaminaCostPerSecond;
    public float MinStaminaToStartSprint => _minStaminaToStartSprint;
    public bool IsGrounded => _isGrounded;
    public bool CanDodge => _dodgeTimer <= 0f;
    public Vector3 VerticalVelocity => _verticalVelocity;

    private void Awake()
    {
        _characterController = GetComponent<CharacterController>();
        _controller = GetComponent<PlayerController>();
        _playerAnimator = GetComponent<PlayerAnimator>();

        // 카메라 참조
        if (Camera.main != null)
        {
            _cameraTransform = Camera.main.transform;
        }
        else
        {
            Debug.LogError("[PlayerMovement] Main Camera not found! Make sure your camera has 'MainCamera' tag.");
        }

        // GroundCheck 자식 찾기
        _groundCheck = transform.Find("GroundCheck");
        if (_groundCheck == null)
        {
            Debug.LogError("[PlayerMovement] 'GroundCheck' child GameObject not found! Please add it under Player.");
        }
    }

    private void Update()
    {
        // 매 프레임 공통 작업 (상태 무관)
        CheckGrounded();
        UpdateDodgeTimer();
        UpdateAnimatorGrounded();
        UpdateVerticalVelocity();
    }

    private void LateUpdate()
    {
        // 이번 프레임 누적된 모든 속도를 합산하여 1회 Move
        Vector3 finalVelocity = _pendingHorizontalVelocity + _verticalVelocity;
        _characterController.Move(finalVelocity * Time.deltaTime);

        // 다음 프레임 위해 수평 속도 초기화 (수직 속도는 중력 누적용이라 유지)
        _pendingHorizontalVelocity = Vector3.zero;
    }

    // ========================================================================
    // === Public API (각 상태가 호출) ===
    // ========================================================================

    /// <summary>
    /// 이번 프레임에 적용할 수평 이동을 요청한다.
    /// 여러 번 호출하면 누적되며, LateUpdate 에서 1회 Move 로 적용된다.
    /// </summary>
    public void RequestMovement(Vector3 horizontalVelocity)
    {
        _pendingHorizontalVelocity += horizontalVelocity;
    }

    /// <summary>
    /// 캐릭터를 주어진 방향으로 부드럽게 회전시킨다 (Slerp 보간).
    /// </summary>
    public void ApplyRotation(Vector3 direction)
    {
        if (direction.sqrMagnitude < MovementInputThreshold) return;

        Quaternion targetRotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _rotationSpeed * Time.deltaTime
        );
    }

    /// <summary>
    /// 캐릭터를 주어진 방향으로 즉시 회전시킨다 (회피 시작 등 즉각 반응이 필요할 때).
    /// </summary>
    public void SetRotationImmediate(Vector3 direction)
    {
        if (direction.sqrMagnitude < MovementInputThreshold) return;
        transform.rotation = Quaternion.LookRotation(direction);
    }

    /// <summary>
    /// 2D 입력 벡터를 카메라 기준 월드 방향으로 변환한다.
    /// </summary>
    public Vector3 GetCameraRelativeDirection(Vector2 input)
    {
        if (input.sqrMagnitude < MovementInputThreshold)
            return Vector3.zero;

        Vector3 cameraForward = _cameraTransform.forward;
        Vector3 cameraRight = _cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        return (cameraForward * input.y + cameraRight * input.x).normalized;
    }

    /// <summary>
    /// 점프 발동. 수직 속도를 점프 높이에 맞춰 설정한다.
    /// JumpState 의 OnEnter 에서 호출.
    /// </summary>
    public void Jump()
    {
        _verticalVelocity.y = Mathf.Sqrt(-2f * _gravity * _jumpHeight);
    }

    /// <summary>
    /// 회피 쿨다운을 시작한다. DodgeState 의 OnExit 에서 호출.
    /// </summary>
    public void StartDodgeCooldown()
    {
        _dodgeTimer = _dodgeCooldown;
    }

    // ========================================================================
    // === Internal Updates ===
    // ========================================================================

    private void CheckGrounded()
    {
        if (_groundCheck == null) return;

        _isGrounded = Physics.CheckSphere(
            _groundCheck.position,
            _groundCheckRadius,
            _groundLayer
        );
    }

    private void UpdateDodgeTimer()
    {
        if (_dodgeTimer > 0f)
            _dodgeTimer -= Time.deltaTime;
    }

    private void UpdateAnimatorGrounded()
    {
        _playerAnimator.SetGrounded(_isGrounded);
    }

    private void UpdateVerticalVelocity()
    {
        if (_isGrounded && _verticalVelocity.y < 0f)
        {
            _verticalVelocity.y = GroundedYVelocity;
        }
        else
        {
            _verticalVelocity.y += _gravity * Time.deltaTime;
        }
    }

    // ========================================================================
    // === Editor Visualization ===
    // ========================================================================

    private void OnDrawGizmosSelected()
    {
        if (_groundCheck == null) return;

        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(_groundCheck.position, _groundCheckRadius);
    }
}