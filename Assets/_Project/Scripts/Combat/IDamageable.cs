using UnityEngine;

/// <summary>
/// 데미지 정보를 담는 구조체.
/// HitBox에서 생성하여 IDamageable에 전달합니다.
/// </summary>
public struct DamageData
{
    /// <summary>데미지 양</summary>
    public float Amount;

    /// <summary>데미지 타입 (물리/마법/트루)</summary>
    public Define.DamageType Type;

    /// <summary>넉백 방향 및 세기</summary>
    public Vector3 KnockbackDirection;
    public float KnockbackForce;

    /// <summary>공격자 GameObject (누가 때렸는지)</summary>
    public GameObject Attacker;

    /// <summary>히트 포인트 (이펙트 위치용)</summary>
    public Vector3 HitPoint;

    /// <summary>히트스톱 적용 여부</summary>
    public bool ApplyHitStop;

    public DamageData(
        float amount,
        Define.DamageType type,
        GameObject attacker,
        Vector3 hitPoint,
        Vector3 knockbackDir,
        float knockbackForce = 0f,
        bool applyHitStop = true)
    {
        Amount = amount;
        Type = type;
        Attacker = attacker;
        HitPoint = hitPoint;
        KnockbackDirection = knockbackDir;
        KnockbackForce = knockbackForce;
        ApplyHitStop = applyHitStop;
    }
}

/// <summary>
/// 데미지를 받을 수 있는 모든 대상이 구현하는 인터페이스.
/// 플레이어, 적, 파괴 가능 오브젝트 등이 구현합니다.
/// 
/// [면접 포인트]
/// 인터페이스 기반이므로 HitBox는 상대가 적인지 플레이어인지
/// 모른 채로 데미지를 줄 수 있습니다 (다형성).
/// </summary>
public interface IDamageable
{
    /// <summary>현재 체력</summary>
    float CurrentHp { get; }

    /// <summary>최대 체력</summary>
    float MaxHp { get; }

    /// <summary>살아있는지 여부</summary>
    bool IsAlive { get; }

    /// <summary>데미지를 받습니다. 실제 적용된 데미지를 반환합니다.</summary>
    float TakeDamage(DamageData data);
}