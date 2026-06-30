using UnityEngine;

/// <summary>
/// Model GameObject 에 부착되어 Animation Event 를 받는 라우터.
/// Enemy 관련 모든 Animation Event 를 한 곳에서 수신하여 적절한 컴포넌트로 분배한다.
/// PlayerAnimationEventReceiver 와 같은 패턴.
/// 
/// 라우팅 대상:
/// - EnemyAnimator: 애니메이션 종료 추적 (Attack, Death)
/// - IEnemyAttacker: 공격 타격 처리 (OnAttackHit). 근접(EnemyAttacker)/원거리(RangedEnemyAttacker) 공통.
/// 
/// IEnemyAttacker 인터페이스 참조로 근접/원거리 구분 없이 PerformHit 라우팅.
/// </summary>
public class EnemyAnimationEventReceiver : MonoBehaviour
{
    private EnemyAnimator _enemyAnimator;
    private IEnemyAttacker _attacker;

    private void Awake()
    {
        _enemyAnimator = GetComponentInParent<EnemyAnimator>();
        _attacker = GetComponentInParent<IEnemyAttacker>();

        if (_enemyAnimator == null)
        {
            Debug.LogError("[EnemyAnimationEventReceiver] EnemyAnimator not found in parent!");
        }

        if (_attacker == null)
        {
            Debug.LogError("[EnemyAnimationEventReceiver] IEnemyAttacker not found in parent!");
        }
    }

    // ========================================================================
    // === Animation Event Callbacks ===
    // ========================================================================

    /// <summary>
    /// 공격 애니메이션 전체 종료 시점에 호출.
    /// </summary>
    public void OnAttackAnimationEnd()
    {
        _enemyAnimator?.OnAttackAnimationFinished();
    }

    /// <summary>
    /// 공격 타격 시점에 호출.
    /// 근접: OverlapSphere 즉시 타격. 원거리: 발사체 생성.
    /// IEnemyAttacker 구현체 (EnemyAttacker / RangedEnemyAttacker) 가 처리.
    /// </summary>
    public void OnAttackHit()
    {
        _attacker?.PerformHit();

        var am = AudioManager.Instance;
        am?.PlaySound(am.Library.EnemyAttack);        // 적 공격음
    }

    /// <summary>
    /// 사망 애니메이션 전체 종료 시점에 호출.
    /// DeathState 가 IsDeathFinished 감지 → SetActive(false).
    /// </summary>
    public void OnDeathAnimationEnd()
    {
        _enemyAnimator?.OnDeathAnimationFinished();
    }

    /// <summary>
    /// 활 발사(릴리즈) 시점에 호출. 보스 전용 (BossAttacker.FireArrow).
    /// 근접 PerformHit 과 분리 - 활 클립은 OnAttackHit 대신 이 이벤트를 쓴다.
    /// 비보스 적은 _attacker 가 BossAttacker 가 아니므로 cast 가 null → 무시 (안전).
    /// </summary>
    public void OnBowRelease()
    {
        (_attacker as BossAttacker)?.FireArrow();

        var am = AudioManager.Instance;
        am?.PlaySound(am.Library.EnemyAttack);        // 적 공격음
    }
}