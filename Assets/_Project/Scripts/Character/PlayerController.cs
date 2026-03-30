using UnityEngine;

/// <summary>
/// 3인칭 액션 RPG 플레이어 컨트롤러.
/// CharacterController 기반으로 이동, 회전, 중력, 점프를 처리합니다.
/// 
/// [의존성]
/// - CharacterController 컴포넌트 필요
/// - Animator 컴포넌트 필요
/// - 카메라의 Transform을 기준으로 이동 방향을 계산
/// 
/// [설계 의도]
/// - Rigidbody 대신 CharacterController를 사용한 이유:
///   액션 RPG에서는 물리 시뮬레이션보다 즉각적인 입력 반응이 중요합니다.
///   CharacterController는 Move()로 직접 이동하므로 정밀한 제어가 가능합니다.
/// </summary>
[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  Inspector 설정값
    // ════════════════════════════════════════════════════

    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _runSpeed = 5.5f;
    [SerializeField] private float _rotationSpeed = 10f;
    [SerializeField] private float _acceleration = 10f;
    [SerializeField] private float _deceleration = 15f;

    [Header("Jump")]
    [SerializeField] private float _jumpHeight = 1.2f;
    [SerializeField] private float _coyoteTime = 0.15f;
    [SerializeField] private float _jumpBufferTime = 0.2f;

    [Header("Gravity")]
    [SerializeField] private float _gravity = -20f;
    [SerializeField] private float _groundedGravity = -2f;
    [SerializeField] private float _fallMultiplier = 2.5f;

    [Header("Ground Check")]
    [SerializeField] private float _groundCheckRadius = 0.28f;
    [SerializeField] private float _groundCheckOffset = 0.1f;
    [SerializeField] private LayerMask _groundLayer;

    // ════════════════════════════════════════════════════
    //  캐싱된 컴포넌트
    // ════════════════════════════════════════════════════

    private CharacterController _controller;
    private Animator _animator;
    private Transform _cameraTransform;

    // ════════════════════════════════════════════════════
    //  내부 상태
    // ════════════════════════════════════════════════════

    private Vector3 _velocity;
    private Vector3 _moveDirection;
    private float _currentSpeed;
    private float _targetSpeed;
    private float _verticalVelocity;

    // 점프 관련
    private bool _isGrounded;
    private float _lastGroundedTime;
    private float _lastJumpRequestTime;
    private bool _jumpRequested;

    // 입력값 (외부에서 설정)
    private Vector2 _inputMove;
    private bool _inputRun;
    private bool _inputJumpPressed;

    // 외부 시스템에서 이동을 막을 때 사용 (공격, 피격 중 등)
    private bool _canMove = true;

    // ════════════════════════════════════════════════════
    //  프로퍼티 (외부 읽기용)
    // ════════════════════════════════════════════════════

    /// <summary>현재 이동 속도 (0~1 정규화 값, 애니메이션 블렌딩용)</summary>
    public float NormalizedSpeed => _currentSpeed / _runSpeed;

    /// <summary>현재 땅에 있는지 여부</summary>
    public bool IsGrounded => _isGrounded;

    /// <summary>현재 실제 이동 속도</summary>
    public float CurrentSpeed => _currentSpeed;

    /// <summary>이동 방향 벡터 (정규화)</summary>
    public Vector3 MoveDirection => _moveDirection;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        _controller = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();
    }

    private void Start()
    {
        // 메인 카메라 캐싱
        if (Camera.main != null)
            _cameraTransform = Camera.main.transform;
        else
            Debug.LogError("[PlayerController] Main Camera를 찾을 수 없습니다.");

        // Ground Layer가 설정 안 되어 있으면 기본값 사용
        if (_groundLayer == 0)
            _groundLayer = Define.Layer.GroundMask;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ════════════════════════════════════════════════════
    //  입력 수신 (Input System에서 호출)
    // ════════════════════════════════════════════════════

    /// <summary>이동 입력을 설정합니다. Input System에서 호출합니다.</summary>
    public void SetMoveInput(Vector2 input)
    {
        _inputMove = input;
    }

    /// <summary>달리기 입력을 설정합니다.</summary>
    public void SetRunInput(bool isRunning)
    {
        _inputRun = isRunning;
    }

    /// <summary>점프 입력을 설정합니다. 버튼 눌린 프레임에 호출합니다.</summary>
    public void SetJumpInput()
    {
        _inputJumpPressed = true;
        _lastJumpRequestTime = Time.time;
    }

    /// <summary>이동 가능 여부를 외부에서 제어합니다. (FSM에서 사용)</summary>
    public void SetCanMove(bool canMove)
    {
        _canMove = canMove;

        // 이동 불가 시 입력 초기화
        if (!canMove)
        {
            _inputMove = Vector2.zero;
            _targetSpeed = 0f;
        }
    }

    // ════════════════════════════════════════════════════
    //  메인 업데이트 루프
    // ════════════════════════════════════════════════════

    private void Update()
    {
        // ── 임시 키보드 입력 (Input System 연동 전 테스트용) ──
        HandleTemporaryInput();

        // ── 핵심 로직 순서 ──
        CheckGround();
        HandleMovement();
        HandleRotation();
        HandleJump();
        ApplyGravity();
        ApplyFinalMovement();
        UpdateAnimator();
    }

    // ════════════════════════════════════════════════════
    //  임시 입력 처리 (Input System 연동 전까지 사용)
    // ════════════════════════════════════════════════════

    private void HandleTemporaryInput()
    {
        // WASD 이동
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        _inputMove = new Vector2(h, v);

        // Shift 달리기
        _inputRun = Input.GetKey(KeyCode.LeftShift);

        // Space 점프
        if (Input.GetKeyDown(KeyCode.Space))
            SetJumpInput();
    }

    // ════════════════════════════════════════════════════
    //  바닥 체크
    // ════════════════════════════════════════════════════

    private void CheckGround()
    {
        Vector3 spherePosition = transform.position + Vector3.up * _groundCheckOffset;

        _isGrounded = Physics.CheckSphere(
            spherePosition,
            _groundCheckRadius,
            _groundLayer,
            QueryTriggerInteraction.Ignore
        );

        if (_isGrounded)
            _lastGroundedTime = Time.time;
    }

    // ════════════════════════════════════════════════════
    //  이동 처리
    // ════════════════════════════════════════════════════

    private void HandleMovement()
    {
        if (!_canMove)
        {
            // 이동 불가 시 감속만 적용
            _currentSpeed = Mathf.MoveTowards(_currentSpeed, 0f, _deceleration * Time.deltaTime);
            return;
        }

        // 입력 크기 계산
        float inputMagnitude = Mathf.Clamp01(_inputMove.magnitude);

        if (inputMagnitude > 0.1f)
        {
            // 카메라 기준으로 이동 방향 계산
            Vector3 forward = _cameraTransform.forward;
            Vector3 right = _cameraTransform.right;

            // Y축 제거 (수평 이동만)
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            // 최종 이동 방향
            _moveDirection = (forward * _inputMove.y + right * _inputMove.x).normalized;

            // 목표 속도 결정
            _targetSpeed = _inputRun ? _runSpeed : _walkSpeed;

            // 부드러운 가속
            _currentSpeed = Mathf.MoveTowards(
                _currentSpeed,
                _targetSpeed * inputMagnitude,
                _acceleration * Time.deltaTime
            );
        }
        else
        {
            // 입력 없으면 감속
            _currentSpeed = Mathf.MoveTowards(
                _currentSpeed,
                0f,
                _deceleration * Time.deltaTime
            );
        }
    }

    // ════════════════════════════════════════════════════
    //  회전 처리 (이동 방향으로 부드럽게 회전)
    // ════════════════════════════════════════════════════

    private void HandleRotation()
    {
        if (!_canMove) return;
        if (_moveDirection == Vector3.zero) return;
        if (_currentSpeed < 0.1f) return;

        Quaternion targetRotation = Quaternion.LookRotation(_moveDirection);
        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            targetRotation,
            _rotationSpeed * Time.deltaTime
        );
    }

    // ════════════════════════════════════════════════════
    //  점프 처리 (Coyote Time + Jump Buffer)
    // ════════════════════════════════════════════════════

    private void HandleJump()
    {
        if (!_canMove) return;

        // 점프 버퍼: 버튼을 미리 눌러도 착지 시 점프 실행
        bool hasJumpBuffer = (Time.time - _lastJumpRequestTime) < _jumpBufferTime;

        // 코요테 타임: 발판에서 떨어진 직후에도 점프 가능
        bool hasCoyoteTime = (Time.time - _lastGroundedTime) < _coyoteTime;

        if (_inputJumpPressed || hasJumpBuffer)
        {
            if (_isGrounded || hasCoyoteTime)
            {
                // 점프 공식: v = sqrt(2 * |gravity| * jumpHeight)
                _verticalVelocity = Mathf.Sqrt(2f * Mathf.Abs(_gravity) * _jumpHeight);

                // 버퍼 초기화
                _lastJumpRequestTime = -1f;
                _lastGroundedTime = -1f;
            }
        }

        _inputJumpPressed = false;
    }

    // ════════════════════════════════════════════════════
    //  중력 적용
    // ════════════════════════════════════════════════════

    private void ApplyGravity()
    {
        if (_isGrounded && _verticalVelocity < 0f)
        {
            // 바닥에 있을 때 약한 하향 중력 (바닥에 붙어있도록)
            _verticalVelocity = _groundedGravity;
        }
        else
        {
            // 하강 시 가속 (더 묵직한 낙하감)
            float multiplier = _verticalVelocity < 0f ? _fallMultiplier : 1f;
            _verticalVelocity += _gravity * multiplier * Time.deltaTime;
        }
    }

    // ════════════════════════════════════════════════════
    //  최종 이동 적용
    // ════════════════════════════════════════════════════

    private void ApplyFinalMovement()
    {
        // 수평 이동 + 수직 이동 합산
        Vector3 horizontalMove = _moveDirection * _currentSpeed;
        Vector3 finalMove = new Vector3(
            horizontalMove.x,
            _verticalVelocity,
            horizontalMove.z
        );

        _controller.Move(finalMove * Time.deltaTime);
    }

    // ════════════════════════════════════════════════════
    //  애니메이터 파라미터 업데이트
    // ════════════════════════════════════════════════════

    private void UpdateAnimator()
    {
        // Define.cs의 캐싱된 해시 사용
        _animator.SetFloat(Define.AnimParam.Speed, NormalizedSpeed, 0.1f, Time.deltaTime);
        _animator.SetBool(Define.AnimParam.IsGrounded, _isGrounded);
    }

    // ════════════════════════════════════════════════════
    //  외부에서 호출하는 유틸리티
    // ════════════════════════════════════════════════════

    /// <summary>
    /// 특정 방향으로 즉시 이동시킵니다. (넉백 등에 사용)
    /// </summary>
    public void AddForce(Vector3 force)
    {
        _velocity += force;
    }

    /// <summary>
    /// 이동 속도를 즉시 0으로 만듭니다. (상태 전환 시 사용)
    /// </summary>
    public void StopMovement()
    {
        _currentSpeed = 0f;
        _inputMove = Vector2.zero;
    }

    // ════════════════════════════════════════════════════
    //  디버그 시각화
    // ════════════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        // Ground Check 범위 시각화
        Vector3 spherePosition = transform.position + Vector3.up * _groundCheckOffset;
        Gizmos.color = _isGrounded ? Color.green : Color.red;
        Gizmos.DrawWireSphere(spherePosition, _groundCheckRadius);
    }
}
