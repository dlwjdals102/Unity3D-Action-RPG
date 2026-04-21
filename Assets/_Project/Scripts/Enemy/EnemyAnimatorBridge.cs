using UnityEngine;

/// <summary>
/// 적 Animator의 Animation Event를 받아 HitBox를 제어합니다.
/// 
/// [사용법]
/// 1. 적 오브젝트에 이 컴포넌트 부착 (Animator와 같은 오브젝트)
/// 2. Inspector에서 Attack HitBox 연결
/// 3. 공격 애니메이션의 특정 프레임에 Animation Event 추가:
///    - 타격 시작 프레임: OnAttackHitStart
///    - 타격 종료 프레임: OnAttackHitEnd
///    - 애니메이션 마지막 프레임: OnAttackAnimationEnd
/// [장점]
/// Invoke 타이머 방식과 달리, 애니메이션이 중단되면 이벤트도 발생하지 않으므로
/// Hit/Die 상태로 전환될 때 HitBox가 켜질 위험이 없습니다.
/// </summary>
public class EnemyAnimatorBridge : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HitBox _attackHitBox;

    /// <summary>공격 애니메이션 종료 시 발생 (EnemyAI가 구독)</summary>
    public event System.Action OnAttackEnd;

    /// <summary>피격 애니메이션 종료 시 발생 (EnemyAI가 구독)</summary>
    public event System.Action OnHitEnd;

    /// <summary>Animation Event: 공격 타격 시작 프레임</summary>
    public void OnAttackHitStart()
    {
        _attackHitBox?.EnableHitBox();
    }

    /// <summary>Animation Event: 공격 타격 종료 프레임</summary>
    public void OnAttackHitEnd()
    {
        _attackHitBox?.DisableHitBox();
    }

    /// <summary>Animation Event: 공격 애니메이션 완전 종료 (Transition 직전)</summary>
    public void OnAttackAnimationEnd()
    {
        OnAttackEnd?.Invoke();
    }

    /// <summary>Animation Event: 피격 애니메이션 완전 종료</summary>
    public void OnHitAnimationEnd()
    {
        OnHitEnd?.Invoke();
    }
}