using UnityEngine;

/// <summary>
/// 적의 사망 상태.
/// EnemyHealth.OnDeath 이벤트 발행 시 EnemyStateMachine.HandleDeath 가 강제 전환.
/// 
/// 동작:
/// - OnEnter: 이동 정지 + 콜라이더 비활성 (추가 데미지 차단) + Death 애니메이션 재생
/// - OnUpdate: Death 애니메이션 종료 감지 → SetActive(false)
/// 
/// 미래 확장 가능:
/// - 시체 잠시 잔존 후 사라짐 (옵션 Y)
/// - 영혼/아이템 드롭 (Week 11)
/// - 사망 효과음 (SoundManager 도입 시)
/// </summary>
public class EnemyDeathState : EnemyStateBase
{
    public EnemyDeathState(EnemyStateMachine stateMachine) : base(stateMachine) { }

    public override void OnEnter()
    {
        // 1. 이동 정지 (NavMeshAgent 정지)
        _stateMachine.Movement.StopMoving();

        // 2. 콜라이더 비활성 (추가 데미지 차단 + OverlapSphere 감지 차단)
        var collider = _stateMachine.GetComponent<Collider>();
        if (collider != null)
        {
            collider.enabled = false;
        }

        // 3. Animator MoveSpeed 0 (Idle 자세, Locomotion 영향 차단)
        _stateMachine.Animator.SetMoveSpeed(0f);

        // 4. Death 애니메이션 재생
        _stateMachine.Animator.PlayDeath();
    }

    public override void OnUpdate()
    {
        // Death 애니메이션 종료 감지
        // Animation Event (OnDeathAnimationEnd) 가 EnemyAnimator.IsDeathFinished = true 설정
        if (_stateMachine.Animator.IsDeathFinished)
        {
            // 적 GameObject 비활성 (시야에서 사라짐)
            _stateMachine.gameObject.SetActive(false);
        }
    }
}