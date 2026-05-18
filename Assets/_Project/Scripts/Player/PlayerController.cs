using UnityEngine;

/// <summary>
/// 플레이어의 입력을 받아 다른 컴포넌트(이동, 전투 등)에 전달하는 컨트롤러.
/// 입력 처리만 담당하며, 실제 이동/회전 로직은 PlayerMovement 와 PlayerStateMachine 에 위임한다.
/// 트리거성 입력(점프, 회피, 공격)은 한 프레임 동안만 유효한 플래그로 노출한다.
/// </summary>
[RequireComponent(typeof(PlayerMovement))]
public class PlayerController : MonoBehaviour
{
    // === References ===
    private PlayerMovement _movement;

    // === Input ===
    private PlayerInputActions _inputActions;

    // === Input State (다른 컴포넌트가 읽어가는 값) ===
    private Vector2 _moveInput;
    private Vector2 _lookInput;
    private bool _isSprintHeld;

    // === Trigger Flags (한 프레임 동안만 유효, 각 상태가 읽고 LateUpdate 에서 자동 리셋) ===
    private bool _jumpRequested;
    private bool _dodgeRequested;
    private bool _attackRequested;

    // === Public Read-Only Access ===
    public Vector2 MoveInput => _moveInput;
    public Vector2 LookInput => _lookInput;
    public bool IsSprintHeld => _isSprintHeld;
    public bool JumpRequested => _jumpRequested;
    public bool DodgeRequested => _dodgeRequested;
    public bool AttackRequested => _attackRequested;

    private void Awake()
    {
        // 컴포넌트 참조 가져오기
        _movement = GetComponent<PlayerMovement>();

        // Input Actions 인스턴스 생성
        _inputActions = new PlayerInputActions();

        // 연속 입력 (Move, Look)
        _inputActions.Player.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _inputActions.Player.Move.canceled += ctx => _moveInput = Vector2.zero;

        _inputActions.Player.Look.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
        _inputActions.Player.Look.canceled += ctx => _lookInput = Vector2.zero;

        // 홀드 입력 (Sprint)
        _inputActions.Player.Sprint.performed += ctx => _isSprintHeld = true;
        _inputActions.Player.Sprint.canceled += ctx => _isSprintHeld = false;

        // 트리거 입력 (Dodge, Jump, Attack) - 누르는 순간 1번 발동
        _inputActions.Player.Dodge.performed += ctx => OnDodgePressed();
        _inputActions.Player.Jump.performed += ctx => OnJumpPressed();
        _inputActions.Player.Attack.performed += ctx => OnAttackPressed();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Player.Disable();
    }

    private void LateUpdate()
    {
        // 트리거 플래그를 한 프레임만 유효하도록 리셋
        // 각 상태가 OnUpdate 에서 읽은 후 자동으로 false 가 됨
        _jumpRequested = false;
        _dodgeRequested = false;
        _attackRequested = false;
    }

    private void OnDodgePressed()
    {
        // 회피 요청 플래그 설정. 상태머신(IdleState/MoveState/LandState)이 OnUpdate 에서 감지하여 처리.
        _dodgeRequested = true;
    }

    private void OnJumpPressed()
    {
        // 점프 요청 플래그 설정. 상태머신(IdleState/MoveState/LandState)이 OnUpdate 에서 감지하여 처리.
        _jumpRequested = true;
    }

    private void OnAttackPressed()
    {
        // 공격 요청 플래그 설정. 상태머신(IdleState/MoveState/LandState/AttackState)이 OnUpdate 에서 감지하여 처리.
        _attackRequested = true;
    }
}