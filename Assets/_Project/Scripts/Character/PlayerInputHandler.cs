using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// New Input System과 PlayerController 사이의 브릿지.
/// Input Actions의 콜백을 받아서 PlayerController의 메서드를 호출합니다.
/// 
/// [설계 의도]
/// - PlayerController는 입력 소스를 모릅니다 (키보드인지 게임패드인지)
/// - 이 클래스만 InputSystem에 의존하므로, 입력 방식 변경 시 이 파일만 수정
/// - 입력 버퍼링: 공격/회피 입력을 큐에 저장하여 FSM에서 소비
/// 
/// [의존성]
/// - PlayerInputActions 에셋 (Generate C# Class 필요)
/// - PlayerController 컴포넌트 (같은 GameObject)
/// </summary>
[RequireComponent(typeof(PlayerController))]
public class PlayerInputHandler : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  캐싱
    // ════════════════════════════════════════════════════

    private PlayerInputActions _inputActions;
    private PlayerController _controller;

    // ════════════════════════════════════════════════════
    //  입력 버퍼 (FSM에서 소비)
    //  전투 시스템에서 "공격 버튼이 눌렸는가?"를 확인할 때 사용
    // ════════════════════════════════════════════════════

    /// <summary>공격 입력이 버퍼에 있는지</summary>
    public bool AttackBuffered { get; private set; }

    /// <summary>강공격 입력이 버퍼에 있는지</summary>
    public bool HeavyAttackBuffered { get; private set; }

    /// <summary>회피 입력이 버퍼에 있는지</summary>
    public bool DodgeBuffered { get; private set; }

    /// <summary>스킬1 입력이 버퍼에 있는지</summary>
    public bool Skill1Buffered { get; private set; }

    /// <summary>스킬2 입력이 버퍼에 있는지</summary>
    public bool Skill2Buffered { get; private set; }

    /// <summary>상호작용 입력이 버퍼에 있는지</summary>
    public bool InteractBuffered { get; private set; }

    /// <summary>현재 이동 입력값 (Vector2)</summary>
    public Vector2 MoveInput { get; private set; }

    /// <summary>현재 달리기 중인지</summary>
    public bool IsRunning { get; private set; }

    // ════════════════════════════════════════════════════
    //  초기화 / 해제
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        _controller = GetComponent<PlayerController>();
        _inputActions = new PlayerInputActions();
    }

    private void OnEnable()
    {
        _inputActions.Player.Enable();
        BindActions();
    }

    private void OnDisable()
    {
        UnbindActions();
        _inputActions.Player.Disable();
    }

    private void OnDestroy()
    {
        _inputActions?.Dispose();
    }

    // ════════════════════════════════════════════════════
    //  액션 바인딩
    // ════════════════════════════════════════════════════

    private void BindActions()
    {
        // 연속 입력 (Value)
        _inputActions.Player.Move.performed += OnMove;
        _inputActions.Player.Move.canceled += OnMove;

        _inputActions.Player.Run.performed += OnRun;
        _inputActions.Player.Run.canceled += OnRun;

        // 단발 입력 (Button)
        _inputActions.Player.Attack.performed += OnAttack;
        _inputActions.Player.HeavyAttack.performed += OnHeavyAttack;
        _inputActions.Player.Dodge.performed += OnDodge;
        _inputActions.Player.Jump.performed += OnJump;
        _inputActions.Player.Skill1.performed += OnSkill1;
        _inputActions.Player.Skill2.performed += OnSkill2;
        _inputActions.Player.Interact.performed += OnInteract;
    }

    private void UnbindActions()
    {
        _inputActions.Player.Move.performed -= OnMove;
        _inputActions.Player.Move.canceled -= OnMove;

        _inputActions.Player.Run.performed -= OnRun;
        _inputActions.Player.Run.canceled -= OnRun;

        _inputActions.Player.Attack.performed -= OnAttack;
        _inputActions.Player.HeavyAttack.performed -= OnHeavyAttack;
        _inputActions.Player.Dodge.performed -= OnDodge;
        _inputActions.Player.Jump.performed -= OnJump;
        _inputActions.Player.Skill1.performed -= OnSkill1;
        _inputActions.Player.Skill2.performed -= OnSkill2;
        _inputActions.Player.Interact.performed -= OnInteract;
    }

    // ════════════════════════════════════════════════════
    //  콜백 핸들러 — 연속 입력
    // ════════════════════════════════════════════════════

    private void OnMove(InputAction.CallbackContext ctx)
    {
        MoveInput = ctx.ReadValue<Vector2>();
        _controller.SetMoveInput(MoveInput);
    }

    private void OnRun(InputAction.CallbackContext ctx)
    {
        // performed = 눌림, canceled = 뗌
        IsRunning = ctx.performed;
        _controller.SetRunInput(IsRunning);
    }

    // ════════════════════════════════════════════════════
    //  콜백 핸들러 — 단발 입력 (버퍼링)
    // ════════════════════════════════════════════════════

    private void OnAttack(InputAction.CallbackContext ctx)
    {
        AttackBuffered = true;
    }

    private void OnHeavyAttack(InputAction.CallbackContext ctx)
    {
        HeavyAttackBuffered = true;
    }

    private void OnDodge(InputAction.CallbackContext ctx)
    {
        DodgeBuffered = true;
    }

    private void OnJump(InputAction.CallbackContext ctx)
    {
        _controller.SetJumpInput();
    }

    private void OnSkill1(InputAction.CallbackContext ctx)
    {
        Skill1Buffered = true;
    }

    private void OnSkill2(InputAction.CallbackContext ctx)
    {
        Skill2Buffered = true;
    }

    private void OnInteract(InputAction.CallbackContext ctx)
    {
        InteractBuffered = true;
    }

    // ════════════════════════════════════════════════════
    //  버퍼 소비 메서드 (FSM에서 호출)
    //  
    //  사용 예시 (4단계 FSM에서):
    //    if (_inputHandler.ConsumeAttack())
    //        TransitionTo(CharacterState.Attack);
    // ════════════════════════════════════════════════════

    /// <summary>공격 버퍼를 소비합니다. 호출 시 버퍼가 초기화됩니다.</summary>
    public bool ConsumeAttack()
    {
        if (!AttackBuffered) return false;
        AttackBuffered = false;
        return true;
    }

    /// <summary>강공격 버퍼를 소비합니다.</summary>
    public bool ConsumeHeavyAttack()
    {
        if (!HeavyAttackBuffered) return false;
        HeavyAttackBuffered = false;
        return true;
    }

    /// <summary>회피 버퍼를 소비합니다.</summary>
    public bool ConsumeDodge()
    {
        if (!DodgeBuffered) return false;
        DodgeBuffered = false;
        return true;
    }

    /// <summary>스킬1 버퍼를 소비합니다.</summary>
    public bool ConsumeSkill1()
    {
        if (!Skill1Buffered) return false;
        Skill1Buffered = false;
        return true;
    }

    /// <summary>스킬2 버퍼를 소비합니다.</summary>
    public bool ConsumeSkill2()
    {
        if (!Skill2Buffered) return false;
        Skill2Buffered = false;
        return true;
    }

    /// <summary>상호작용 버퍼를 소비합니다.</summary>
    public bool ConsumeInteract()
    {
        if (!InteractBuffered) return false;
        InteractBuffered = false;
        return true;
    }

    /// <summary>모든 버퍼를 초기화합니다. (상태 전환 시 사용)</summary>
    public void ClearAllBuffers()
    {
        AttackBuffered = false;
        HeavyAttackBuffered = false;
        DodgeBuffered = false;
        Skill1Buffered = false;
        Skill2Buffered = false;
        InteractBuffered = false;
    }
}