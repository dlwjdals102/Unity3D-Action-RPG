using UnityEngine;

/// <summary>
/// 플레이어의 이동, 회전, 점프, 회피, 중력, 지면 감지를 담당.
/// 입력은 PlayerController, 애니메이션은 PlayerAnimator 에 위임한다.
/// 모든 이동 계산은 누적 후 Update 마지막에 단 한 번의 Move 로 처리한다.
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
    [SerializeField] private float _dodgeDistance = 4f;
    [SerializeField] private float _dodgeDuration = 0.6f;
    [SerializeField] private float _dodgeCooldown = 0.2f;
    [SerializeField] private AnimationCurve _dodgeSpeedCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0f);

    // === Ground Check ===
    [Header("Ground Check")]
    [SerializeField] private float _groundCheckRadius = 0.25f;
    [SerializeField] private LayerMask _groundLayer;

    // === Internal State ===
    private Vector3 _verticalVelocity;
    private bool _isDodging;
    private float _dodgeTimer;
    private float _dodgeStartTime;
    private Vector3 _dodgeDirection;
    private bool _isGrounded;

    // === Public Properties ===
    public bool CanDodge => !_isDodging && _dodgeTimer <= 0f;
    public bool IsGrounded => _isGrounded;

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
        // 1. 상태 갱신
        CheckGrounded();
        UpdateDodgeTimer();
        UpdateAnimatorGrounded();

        // 2. 수평 속도 계산 (회피 또는 일반 이동)
        Vector3 horizontalVelocity = _isDodging
            ? CalculateDodgeVelocity()
            : CalculateMovementVelocity();

        // 3. 수직 속도 누적 (중력)
        UpdateVerticalVelocity();

        // 4. 최종 속도 합산 후 Move 1회 호출
        Vector3 finalVelocity = horizontalVelocity + _verticalVelocity;
        _characterController.Move(finalVelocity * Time.deltaTime);
    }

    // ========================================================================
    // === State Updates ===
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

        // 회피 종료는 CalculateDodgeVelocity 에서 처리
    }

    private void UpdateAnimatorGrounded()
    {
        _playerAnimator.SetGrounded(_isGrounded);
    }

    // ========================================================================
    // === Velocity Calculations (Move 호출 안 함, 속도만 반환) ===
    // ========================================================================

    private Vector3 CalculateMovementVelocity()
    {
        Vector2 input = _controller.MoveInput;

        // 입력이 없으면 정지
        if (input.sqrMagnitude < MovementInputThreshold)
        {
            _playerAnimator.SetMoveSpeed(0f);
            return Vector3.zero;
        }

        // 카메라 기준 방향 계산
        Vector3 cameraForward = _cameraTransform.forward;
        Vector3 cameraRight = _cameraTransform.right;
        cameraForward.y = 0f;
        cameraRight.y = 0f;
        cameraForward.Normalize();
        cameraRight.Normalize();

        // 입력을 월드 방향으로 변환
        Vector3 moveDirection = (cameraForward * input.y + cameraRight * input.x).normalized;

        // 캐릭터 회전 (이동 방향을 바라보도록)
        Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _rotationSpeed * Time.deltaTime
        );

        // 애니메이터 파라미터
        float normalizedSpeed = _controller.IsSprintHeld ? 1f : 0.5f;
        _playerAnimator.SetMoveSpeed(normalizedSpeed);

        // 속도 계산 후 반환
        float currentSpeed = _controller.IsSprintHeld ? _runSpeed : _walkSpeed;
        return moveDirection * currentSpeed;
    }

    private Vector3 CalculateDodgeVelocity()
    {
        float elapsed = Time.time - _dodgeStartTime;
        float t = elapsed / _dodgeDuration;

        // 회피 시간 종료
        if (t >= 1f)
        {
            _isDodging = false;
            return Vector3.zero;
        }

        // 커브에 따른 속도 비율 (시작 빠름, 끝 느림)
        float speedMultiplier = _dodgeSpeedCurve.Evaluate(t);
        float baseSpeed = _dodgeDistance / _dodgeDuration;
        float currentSpeed = baseSpeed * speedMultiplier;

        return _dodgeDirection * currentSpeed;
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
    // === Public API (외부에서 호출) ===
    // ========================================================================

    public void TryJump()
    {
        if (!_isGrounded || _isDodging) return;

        // 점프 공식: v = sqrt(-2 * g * h)
        _verticalVelocity.y = Mathf.Sqrt(-2f * _gravity * _jumpHeight);

        _playerAnimator.PlayJump();
    }

    public void TryDodge()
    {
        if (!CanDodge) return;
        if (!_isGrounded) return;

        _isDodging = true;
        _dodgeStartTime = Time.time;
        _dodgeTimer = _dodgeDuration + _dodgeCooldown;

        // 회피 방향 결정
        Vector2 moveInput = _controller.MoveInput;
        if (moveInput.sqrMagnitude > MovementInputThreshold)
        {
            // 이동 입력이 있으면 그 방향으로 회피
            Vector3 cameraForward = _cameraTransform.forward;
            Vector3 cameraRight = _cameraTransform.right;
            cameraForward.y = 0f;
            cameraRight.y = 0f;
            cameraForward.Normalize();
            cameraRight.Normalize();

            _dodgeDirection = (cameraForward * moveInput.y + cameraRight * moveInput.x).normalized;

            // 회피 시작 시 그 방향을 즉시 바라보도록
            transform.rotation = Quaternion.LookRotation(_dodgeDirection);
        }
        else
        {
            // 입력 없으면 현재 바라보는 방향
            _dodgeDirection = transform.forward;
        }

        _playerAnimator.PlayDodge();
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