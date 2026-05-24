/// <summary>
/// 적의 공격 처리 인터페이스.
/// Animation Event 시점에 PerformHit 이 호출되어 공격을 실행한다.
/// 
/// 구현이 다른 공격 방식을 공통 처리하기 위한 인터페이스:
/// - EnemyAttacker (근접): OverlapSphere 즉시 타격
/// - EnemyRangedAttacker (원거리): 발사체 생성
/// - 미래: 소환, 범위 공격 등
/// 
/// EnemyAnimationEventReceiver 가 IEnemyAttacker 로 참조하여
/// 근접/원거리 구분 없이 OnAttackHit → PerformHit 라우팅.
/// (IDamageable 과 같은 "구현이 다른 것을 인터페이스로" 판단)
/// </summary>
public interface IEnemyAttacker
{
    /// <summary>
    /// 공격 타격 시점에 호출 (Animation Event 가 발사).
    /// 근접: OverlapSphere 검사. 원거리: 발사체 생성.
    /// </summary>
    void PerformHit();
}