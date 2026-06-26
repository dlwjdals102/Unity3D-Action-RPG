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
    private bool _isGuardHeld;

    // === Trigger Flags (한 프레임 동안만 유효, 각 상태가 읽고 LateUpdate 에서 자동 리셋) ===
    private bool _jumpRequested;
    private bool _dodgeRequested;
    private bool _attackRequested;
    private bool _lockOnRequested;
    private bool _switchTargetLeftRequested;
    private bool _switchTargetRightRequested;
    private bool _interactRequested;
    private bool _toggleInventoryRequested;
    private bool _cancelRequested;

    // === Public Read-Only Access ===
    public Vector2 MoveInput => _moveInput;
    public Vector2 LookInput => _lookInput;
    public bool IsSprintHeld => _isSprintHeld;
    public bool IsGuardHeld => _isGuardHeld;
    public bool JumpRequested => _jumpRequested;
    public bool DodgeRequested => _dodgeRequested;
    public bool AttackRequested => _attackRequested;
    public bool LockOnRequested => _lockOnRequested;
    public bool SwitchTargetLeftRequested => _switchTargetLeftRequested;
    public bool SwitchTargetRightRequested => _switchTargetRightRequested;
    public bool InteractRequested => _interactRequested;
    public bool ToggleInventoryRequested => _toggleInventoryRequested;
    public bool CancelRequested => _cancelRequested;

    /// <summary>
    /// Cancel(ESC) 입력을 1회성으로 소비한다. 눌려 있었으면 true 반환 + 플래그를 끈다.
    /// 여러 소비자(상점/인벤토리/Pause)가 같은 ESC에 동시에 반응하지 않도록,
    /// 먼저 처리한 쪽이 소비하면 나머지는 false 를 받는다 (Update 순서 무관).
    /// </summary>
    public bool ConsumeCancel()
    {
        if (!_cancelRequested) return false;
        _cancelRequested = false;
        return true;
    }

    private void Awake()
    {
        // 컴포넌트 참조 가져오기
        _movement = GetComponent<PlayerMovement>();

        // Input Actions 인스턴스 생성
        _inputActions = new PlayerInputActions();

        // === PlayerPersistent (항상 켜짐: 이동 + 패널 조작) ===
        _inputActions.PlayerPersistent.Move.performed += ctx => _moveInput = ctx.ReadValue<Vector2>();
        _inputActions.PlayerPersistent.Move.canceled += ctx => _moveInput = Vector2.zero;
        _inputActions.PlayerPersistent.Interact.performed += ctx => OnInteractPressed();
        _inputActions.PlayerPersistent.ToggleInventory.performed += ctx => OnToggleInventoryPressed();
        _inputActions.PlayerPersistent.Cancel.performed += ctx => OnCancelPressed();

        // === PlayerGameplay (UI 열리면 꺼짐: 카메라/전투/타겟) ===
        _inputActions.PlayerGameplay.Look.performed += ctx => _lookInput = ctx.ReadValue<Vector2>();
        _inputActions.PlayerGameplay.Look.canceled += ctx => _lookInput = Vector2.zero;
        _inputActions.PlayerGameplay.Sprint.performed += ctx => _isSprintHeld = true;
        _inputActions.PlayerGameplay.Sprint.canceled += ctx => _isSprintHeld = false;
        _inputActions.PlayerGameplay.Guard.performed += ctx => _isGuardHeld = true;
        _inputActions.PlayerGameplay.Guard.canceled += ctx => _isGuardHeld = false;
        _inputActions.PlayerGameplay.Dodge.performed += ctx => OnDodgePressed();
        _inputActions.PlayerGameplay.Jump.performed += ctx => OnJumpPressed();
        _inputActions.PlayerGameplay.Attack.performed += ctx => OnAttackPressed();
        _inputActions.PlayerGameplay.LockOn.performed += ctx => OnLockOnPressed();
        _inputActions.PlayerGameplay.SwitchTargetLeft.performed += ctx => OnSwitchTargetLeftPressed();
        _inputActions.PlayerGameplay.SwitchTargetRight.performed += ctx => OnSwitchTargetRightPressed();

        Cursor.visible = false;
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void OnEnable()
    {
        _inputActions.PlayerGameplay.Enable();
        _inputActions.PlayerPersistent.Enable();
        UIInputLock.OnChanged += HandleUILockChanged;
    }

    private void OnDisable()
    {
        _inputActions.PlayerGameplay.Disable();
        _inputActions.PlayerPersistent.Disable();
        UIInputLock.OnChanged -= HandleUILockChanged;
    }

    private void LateUpdate()
    {
        // 트리거 플래그를 한 프레임만 유효하도록 리셋
        // 각 상태가 OnUpdate 에서 읽은 후 자동으로 false 가 됨
        _jumpRequested = false;
        _dodgeRequested = false;
        _attackRequested = false;
        _lockOnRequested = false;
        _switchTargetLeftRequested = false;
        _switchTargetRightRequested = false;
        _interactRequested = false;
        _toggleInventoryRequested = false;
        _cancelRequested = false;
    }

    private void HandleUILockChanged(bool uiOpen)
    {
        if (uiOpen) _inputActions.PlayerGameplay.Disable();  // 카메라·전투 차단 (Persistent 유지 → WASD/닫기 OK)
        else _inputActions.PlayerGameplay.Enable();
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

    private void OnLockOnPressed()
    {
        // 락온 토글 요청. LockOnSystem 이 OnUpdate 에서 감지해 켜기/끄기 판단.
        _lockOnRequested = true;
    }

    private void OnSwitchTargetLeftPressed()
    {
        // 왼쪽 적으로 타겟 전환 요청. LockOnSystem 이 처리.
        _switchTargetLeftRequested = true;
    }

    private void OnSwitchTargetRightPressed()
    {
        // 오른쪽 적으로 타겟 전환 요청. LockOnSystem 이 처리.
        _switchTargetRightRequested = true;
    }

    private void OnInteractPressed()
    {
        // 상호작용 요청 (화톳불 휴식 등). 화톳불이 감지해 처리.
        _interactRequested = true;
    }

    private void OnToggleInventoryPressed()
    {
        _toggleInventoryRequested = true;
    }

    private void OnCancelPressed()
    {
        _cancelRequested = true;
    }
}