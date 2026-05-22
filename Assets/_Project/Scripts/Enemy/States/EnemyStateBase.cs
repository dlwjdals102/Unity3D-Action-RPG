using UnityEngine;

/// <summary>
/// 모든 적 상태의 추상 베이스 클래스.
/// 각 구체 상태 (EnemyPatrolState, EnemyChaseState 등) 는 이 클래스를 상속받아
/// OnEnter, OnUpdate, OnExit 를 필요에 따라 오버라이드한다.
/// PlayerStateBase 와 같은 패턴.
/// </summary>
public abstract class EnemyStateBase
{
    // 모든 상태가 상태머신에 접근 가능 (다른 상태로 전환할 때 필요)
    protected EnemyStateMachine _stateMachine;

    /// <summary>
    /// 생성자에서 상태머신 참조를 주입받는다.
    /// </summary>
    public EnemyStateBase(EnemyStateMachine stateMachine)
    {
        _stateMachine = stateMachine;
    }

    /// <summary>
    /// 상태 진입 시 1번 호출. 애니메이션 재생, 초기 설정 등 사용.
    /// </summary>
    public virtual void OnEnter() { }

    /// <summary>
    /// 매 프레임 호출. 상태 결정 (거리, 시야 등), 이동 처리, 전환 조건 검사 등 사용.
    /// </summary>
    public virtual void OnUpdate() { }

    /// <summary>
    /// 상태 종료 시 1번 호출. 정리 작업에 사용.
    /// </summary>
    public virtual void OnExit() { }
}