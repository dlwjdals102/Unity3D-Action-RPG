using UnityEngine;

/// <summary>
/// Model GameObject 에 부착되어 Animation Event 를 받는 라우터.
/// Enemy 관련 모든 Animation Event 를 한 곳에서 수신하여 적절한 컴포넌트로 분배한다.
/// PlayerAnimationEventReceiver 와 같은 패턴.
/// 
/// 라우팅 대상:
/// - EnemyAnimator: 애니메이션 종료 추적 (Attack, Death)
/// - EnemyAttacker: 공격 타격 처리 (OnAttackHit)
/// 
/// 향후 다른 Animation Event 도 같은 패턴으로 추가 가능.
/// </summary>
public class EnemyAnimationEventReceiver : MonoBehaviour
{
    private EnemyAnimator _enemyAnimator;
    private EnemyAttacker _enemyAttacker;

    private void Awake()
    {
        _enemyAnimator = GetComponentInParent<EnemyAnimator>();
        _enemyAttacker = GetComponentInParent<EnemyAttacker>();

        if (_enemyAnimator == null)
        {
            Debug.LogError("[EnemyAnimationEventReceiver] EnemyAnimator not found in parent!");
        }

        if (_enemyAttacker == null)
        {
            Debug.LogError("[EnemyAnimationEventReceiver] EnemyAttacker not found in parent!");
        }
    }

    // ========================================================================
    // === Animation Event Callbacks ===
    // 각 메서드는 Mixamo 클립의 Animation Event 에서 호출된다.
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
    /// EnemyAttacker 가 OverlapSphere 로 한 프레임 검사하여 Target (Player) 감지 + 데미지 적용.
    /// </summary>
    public void OnAttackHit()
    {
        _enemyAttacker?.PerformHit();
    }

    /// <summary>
    /// 사망 애니메이션 전체 종료 시점에 호출.
    /// DeathState 가 IsDeathFinished 감지 → SetActive(false).
    /// </summary>
    public void OnDeathAnimationEnd()
    {
        _enemyAnimator?.OnDeathAnimationFinished();
    }
}