using UnityEngine;

/// <summary>
/// 범용 상태 머신.
/// 현재 상태를 관리하고 상태 전환을 처리합니다.
/// 
/// [설계]
/// - 한 번에 하나의 상태만 활성
/// - 전환 시 Exit() → Enter() 순서 보장
/// - 같은 상태로의 재진입 방지
/// </summary>
public class StateMachine
{
    public IState CurrentState { get; private set; }
    public IState PreviousState { get; private set; }

    /// <summary>현재 상태의 지속 시간 (초)</summary>
    public float StateTime { get; private set; }

    /// <summary>상태를 전환합니다. 같은 상태면 무시합니다.</summary>
    public void ChangeState(IState newState)
    {
        if (newState == null)
        {
            Debug.LogError("[StateMachine] null 상태로 전환 시도");
            return;
        }

        if (CurrentState == newState) return;

        PreviousState = CurrentState;
        CurrentState?.Exit();

        CurrentState = newState;
        StateTime = 0f;
        CurrentState.Enter();
    }

    /// <summary>매 프레임 호출합니다.</summary>
    public void Update()
    {
        StateTime += Time.deltaTime;
        CurrentState?.Update();
    }

    /// <summary>매 물리 프레임 호출합니다.</summary>
    public void FixedUpdate()
    {
        CurrentState?.FixedUpdate();
    }
}