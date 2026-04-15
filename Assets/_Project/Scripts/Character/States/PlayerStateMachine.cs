using Unity.IO.LowLevel.Unsafe;
using UnityEngine;

/// <summary>
/// Player 전용 상태 머신.
/// 각 상태 인스턴스를 생성/보유하고, Update에서 StateMachine을 구동합니다.
/// 
/// [구조]
/// PlayerStateMachine (이 클래스)
///   ├── StateMachine (범용 상태 관리자)
///   └── 각 State 인스턴스 (IdleState, MoveState, ...)
///       └── PlayerStateContext (공유 데이터 참조)
/// 
/// [면접 포인트]
/// - Context 패턴: 상태들이 필요한 참조(Controller, Animator, Input)를
///   Context 구조체를 통해 받아서 의존성이 명확합니다.
/// - Open/Closed: 새 상태 추가 시 기존 코드 수정 없이 클래스만 추가하면 됩니다.
/// </summary>
[RequireComponent(typeof(PlayerController))]
[RequireComponent(typeof(PlayerAnimator))]
[RequireComponent(typeof(PlayerInputHandler))]
public class PlayerStateMachine : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  공유 컨텍스트 (모든 상태가 참조)
    // ════════════════════════════════════════════════════

    /// <summary>
    /// 상태 클래스들이 접근하는 공유 데이터 묶음.
    /// 각 상태가 PlayerController 등을 직접 GetComponent하지 않고
    /// 이 컨텍스트를 통해 접근합니다.
    /// </summary>
    public struct PlayerStateContext
    {
        public PlayerController Controller;
        public PlayerAnimator Animator;
        public PlayerInputHandler Input;
        public PlayerStateMachine Owner;
        public Transform Transform;
    }

    // ════════════════════════════════════════════════════
    //  프로퍼티
    // ════════════════════════════════════════════════════

    public StateMachine FSM { get; private set; }
    public PlayerStateContext Context { get; private set; }

    /// <summary>현재 상태를 Define.CharacterState enum으로 반환</summary>
    public Define.CharacterState CurrentStateType { get; private set; }

    // ── 각 상태 인스턴스 (외부에서 읽기 가능) ──
    public IdleState IdleState { get; private set; }
    public MoveState MoveState { get; private set; }
    public AttackState AttackState { get; private set; }
    public DodgeState DodgeState { get; private set; }
    public HitState HitState { get; private set; }
    public DieState DieState { get; private set; }
    public SkillState SkillState { get; private set; }

    // ── 플래그 ──
    private bool _isDead = false;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        // 컨텍스트 구성
        Context = new PlayerStateContext
        {
            Controller = GetComponent<PlayerController>(),
            Animator = GetComponent<PlayerAnimator>(),
            Input = GetComponent<PlayerInputHandler>(),
            Owner = this,
            Transform = transform
        };

        // 상태 머신 생성
        FSM = new StateMachine();

        // 각 상태 인스턴스 생성 (Context 주입)
        IdleState = new IdleState(Context);
        MoveState = new MoveState(Context);
        AttackState = new AttackState(Context);
        DodgeState = new DodgeState(Context);
        HitState = new HitState(Context);
        DieState = new DieState(Context);
        SkillState = new SkillState(Context);
    }

    private void Start()
    {
        // Idle 상태에서 시작
        FSM.ChangeState(IdleState);
        CurrentStateType = Define.CharacterState.Idle;
    }

    // ════════════════════════════════════════════════════
    //  업데이트
    // ════════════════════════════════════════════════════

    private void Update()
    {
        if (_isDead) return;

        FSM.Update();
    }

    private void FixedUpdate()
    {
        if (_isDead) return;

        FSM.FixedUpdate();
    }

    // ════════════════════════════════════════════════════
    //  상태 전환 헬퍼 (각 State에서 호출)
    // ════════════════════════════════════════════════════

    /// <summary>
    /// 상태를 전환합니다. enum 기반으로 호출하여 가독성을 높입니다.
    /// </summary>
    public void TransitionTo(Define.CharacterState state)
    {
        if (_isDead && state != Define.CharacterState.Die) return;

        CurrentStateType = state;

        switch (state)
        {
            case Define.CharacterState.Idle:
                FSM.ChangeState(IdleState);
                break;
            case Define.CharacterState.Move:
                FSM.ChangeState(MoveState);
                break;
            case Define.CharacterState.Attack:
                FSM.ChangeState(AttackState);
                break;
            case Define.CharacterState.Dodge:
                FSM.ChangeState(DodgeState);
                break;
            case Define.CharacterState.Hit:
                FSM.ChangeState(HitState);
                break;
            case Define.CharacterState.Die:
                _isDead = true;
                FSM.ChangeState(DieState);
                break;
            case Define.CharacterState.Skill:
                FSM.ChangeState(SkillState);
                break;
        }
    }

    // ════════════════════════════════════════════════════
    //  외부 이벤트 수신
    // ════════════════════════════════════════════════════

    /// <summary>
    /// 데미지를 받았을 때 호출합니다. (Combat 시스템에서 호출)
    /// Dodge(무적) 중이면 무시, Die면 무시.
    /// </summary>
    public void OnTakeDamage(float damage)
    {
        if (_isDead) return;

        TransitionTo(Define.CharacterState.Hit);
    }

    // ════════════════════════════════════════════════════
    //  디버그
    // ════════════════════════════════════════════════════

    private void OnGUI()
    {
#if UNITY_EDITOR
        // 좌상단에 현재 상태 표시 (디버그용)
        GUI.Label(
            new Rect(10, 10, 300, 30),
            $"State: {CurrentStateType} | Time: {FSM.StateTime:F1}s"
        );
#endif
    }
}