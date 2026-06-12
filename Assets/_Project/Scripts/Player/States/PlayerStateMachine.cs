using UnityEngine;

/// <summary>
/// 플레이어의 상태머신 중앙 관리자.
/// 현재 상태를 추적하고 상태 전환을 처리한다.
/// 각 상태는 이 클래스를 통해 다른 컴포넌트(Controller, Movement, Animator, Attacker, Stamina)에 접근한다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerMovement))]
[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(PlayerAttacker))]
[RequireComponent(typeof(PlayerStamina))]
public class PlayerStateMachine : MonoBehaviour
{
    // === Component References (각 상태가 접근) ===
    public PlayerController Controller { get; private set; }
    public PlayerMovement Movement { get; private set; }
    public PlayerAnimator Animator { get; private set; }
    public PlayerAttacker Attacker { get; private set; }
    public PlayerStamina Stamina { get; private set; }
    public LockOnSystem LockOn { get; private set; }
    public PlayerHealth Health { get; private set; }
    public EquipmentManager Equipment { get; private set; }

    // === State Instances (1번만 생성, 재사용) ===
    public IdleState IdleState { get; private set; }
    public MoveState MoveState { get; private set; }
    public JumpState JumpState { get; private set; }
    public FallState FallState { get; private set; }
    public LandState LandState { get; private set; }
    public DodgeState DodgeState { get; private set; }
    public AttackState AttackState { get; private set; }
    public DeathState DeathState { get; private set; }
    public GuardState GuardState { get; private set; }

    // === Current State ===
    public PlayerStateBase CurrentState { get; private set; }

    /// <summary>가드 가능 여부. 방패를 착용한 경우에만 가드할 수 있다 (장비 연동).</summary>
    public bool CanGuard =>
        Equipment != null && Equipment.GetEquipped(EquipmentSlot.Shield) != null;

    private void Awake()
    {
        // 컴포넌트 참조 가져오기
        Controller = GetComponent<PlayerController>();
        Movement = GetComponent<PlayerMovement>();
        Animator = GetComponent<PlayerAnimator>();
        Attacker = GetComponent<PlayerAttacker>();
        Stamina = GetComponent<PlayerStamina>();
        LockOn = GetComponent<LockOnSystem>();
        Health = GetComponent<PlayerHealth>();
        Equipment = GetComponent<EquipmentManager>();

        // 상태 인스턴스 생성
        IdleState = new IdleState(this);
        MoveState = new MoveState(this);
        JumpState = new JumpState(this);
        FallState = new FallState(this);
        LandState = new LandState(this);
        DodgeState = new DodgeState(this);
        AttackState = new AttackState(this);
        DeathState = new DeathState(this);
        GuardState = new GuardState(this);
    }

    private void OnEnable()
    {
        if (Health != null) Health.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (Health != null) Health.OnDeath -= HandleDeath;
    }

    /// <summary>사망 이벤트 수신 → DeathState 전환.</summary>
    private void HandleDeath()
    {
        ChangeState(DeathState);
    }

    private void Start()
    {
        // 초기 상태로 진입
        ChangeState(IdleState);
    }

    private void Update()
    {
        // 현재 상태의 매 프레임 로직 실행
        CurrentState?.OnUpdate();
    }

    /// <summary>
    /// 다른 상태로 전환한다.
    /// 같은 상태로의 전환은 무시되며, OnExit → OnEnter 순서로 호출된다.
    /// </summary>
    public void ChangeState(PlayerStateBase newState)
    {
        // 같은 상태로의 전환 무시 (반복 호출 방지)
        if (newState == CurrentState) return;

        // 이전 상태 종료
        CurrentState?.OnExit();

        // 새 상태로 전환 + 진입
        CurrentState = newState;
        CurrentState.OnEnter();
    }
}