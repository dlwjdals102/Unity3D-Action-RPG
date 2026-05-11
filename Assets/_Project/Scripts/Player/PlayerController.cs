using UnityEngine;

/// <summary>
/// 플레이어의 입력을 받아 다른 컴포넌트(이동, 전투 등)에 전달하는 컨트롤러.
/// 입력 처리만 담당하며, 실제 이동/회전 로직은 PlayerMovement에 위임한다.
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

    // === Public Read-Only Access ===
    public Vector2 MoveInput => _moveInput;
    public Vector2 LookInput => _lookInput;
    public bool IsSprintHeld => _isSprintHeld;

    private void Awake()
    {
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

        // 트리거 입력 (Dodge, Jump) - 누르는 순간 1번 발동
        _inputActions.Player.Dodge.performed += ctx => OnDodgePressed();
        _inputActions.Player.Jump.performed += ctx => OnJumpPressed();
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
    }

    private void OnDisable()
    {
        _inputActions.Player.Disable();
    }

    private void OnDodgePressed()
    {
        _movement.TryDodge();
    }

    private void OnJumpPressed()
    {
        _movement.TryJump();
    }
}