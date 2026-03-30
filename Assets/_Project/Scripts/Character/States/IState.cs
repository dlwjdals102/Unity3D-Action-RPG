using UnityEngine;

/// <summary>
/// FSM 상태 인터페이스.
/// 모든 캐릭터 상태는 이 인터페이스를 구현합니다.
/// 
/// [생명주기]
/// Enter() → Update() (매 프레임) → Exit()
/// 
/// [면접 포인트]
/// 인터페이스 기반 FSM은 상태 추가/제거가 용이하고,
/// 각 상태가 독립적으로 테스트 가능합니다.
/// </summary>
public interface IState
{
    /// <summary>상태 진입 시 1회 호출. 애니메이션 트리거, 변수 초기화 등.</summary>
    void Enter();

    /// <summary>매 프레임 호출. 상태별 로직 및 전환 조건 체크.</summary>
    void Update();

    /// <summary>매 물리 프레임 호출. 물리 기반 처리가 필요한 경우.</summary>
    void FixedUpdate();

    /// <summary>상태 이탈 시 1회 호출. 정리 작업.</summary>
    void Exit();
}