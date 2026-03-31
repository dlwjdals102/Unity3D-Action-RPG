using UnityEngine;

/// <summary>
/// 피격 판정 Collider. 데미지를 받을 수 있는 대상에 부착합니다.
/// HitBox의 Trigger와 충돌하는 역할입니다.
/// 
/// [설정]
/// - Collider를 Trigger가 아닌 일반 Collider로 설정
/// - Layer: Player 또는 Enemy
/// - 부모 또는 자신에 IDamageable 구현 필요
/// 
/// [참고]
/// HurtBox 자체는 로직이 거의 없습니다.
/// HitBox가 OnTriggerEnter에서 GetComponentInParent<IDamageable>()로
/// 데미지를 전달하기 때문입니다.
/// 이 컴포넌트는 "이 오브젝트가 피격 가능하다"는 것을 명시하고,
/// 추가 설정(무적 상태 등)을 관리하기 위해 존재합니다.
/// </summary>
public class HurtBox : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private bool _isInvincible = false;

    /// <summary>무적 상태 여부. Dodge 중 true로 설정합니다.</summary>
    public bool IsInvincible
    {
        get => _isInvincible;
        set => _isInvincible = value;
    }

    private IDamageable _damageable;

    private void Awake()
    {
        _damageable = GetComponentInParent<IDamageable>();

        if (_damageable == null)
            Debug.LogError($"[HurtBox] {gameObject.name}의 부모에 IDamageable이 없습니다.");
    }
}