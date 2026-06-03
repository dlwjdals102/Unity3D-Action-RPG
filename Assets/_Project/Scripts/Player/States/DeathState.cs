using UnityEngine;

/// <summary>
/// 플레이어 사망 상태. PlayerHealth.OnDeath 발행 시 상태머신이 이 상태로 전환한다.
/// 
/// 사망 중에는 입력/이동을 받지 않는다 (OnUpdate 가 비어있어 어떤 입력도 처리 안 함).
/// 부활은 리스폰 시스템이 처리: 체력 복구 후 상태머신을 Idle 로 되돌린다.
/// 
/// 적 DeathState 와 달리 플레이어는 부활하므로 "일시적" 상태다.
/// (사망 모션/연출은 폴리싱 단계에 OnEnter 에서 추가 예정)
/// </summary>
public class DeathState : PlayerStateBase
{
    public DeathState(PlayerStateMachine stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // 이동 정지 (사망 중 미끄러짐 방지)
        _stateMachine.Movement.RequestMovement(Vector3.zero);

        // 사망 모션 (전용 클립은 폴리싱, 지금은 Locomotion 유지)
        _stateMachine.Animator.PlayLocomotion();

        // (폴리싱: 사망 애니메이션 트리거, "YOU DIED" 연출 등)
    }

    public override void OnUpdate()
    {
        // 사망 중에는 어떤 입력도 처리하지 않는다 (가만히).
        // 부활은 외부(리스폰 시스템)가 ResetHealth + 상태머신을 Idle 로 전환.
    }
}