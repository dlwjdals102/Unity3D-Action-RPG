using UnityEngine;

/// <summary>
/// 데미지에 관한 정보를 담는 구조체.
/// IDamageable.TakeDamage 의 매개변수로 사용된다.
/// 값 전달 의도이므로 struct 사용 (GC 부담 없음).
/// </summary>
public struct DamageInfo
{
    /// <summary>데미지 양</summary>
    public int Amount;

    /// <summary>공격자 (누가 공격했는가)</summary>
    public GameObject Source;

    /// <summary>타격 지점 (데미지 텍스트, VFX 위치 등에 활용)</summary>
    public Vector3 HitPoint;
}

/// <summary>
/// 데미지를 받을 수 있는 객체의 인터페이스.
/// 적, 플레이어, 파괴 가능한 오브젝트 등이 구현한다.
/// 단일 책임: 데미지 받기.
/// </summary>
public interface IDamageable
{
    void TakeDamage(DamageInfo info);
}