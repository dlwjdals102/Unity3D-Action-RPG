using UnityEngine;

/// <summary>
/// 소비 아이템 (물약 등). ItemData 를 상속.
/// 사용 시 효과 (체력 회복)를 가진다. 중첩 가능 (MaxStack > 1).
/// 
/// 현재는 체력 회복만. 추후 다른 효과(스태미나 회복, 버프 등)는
/// 필드 추가 또는 효과 타입(enum)으로 확장 가능.
/// </summary>
[CreateAssetMenu(fileName = "Consumable", menuName = "Hollow Blade/Items/Consumable")]
public class ConsumableData : ItemData
{
    [Header("Consumable")]
    [Tooltip("사용 시 체력 회복량")]
    [SerializeField] private int _healAmount = 0;

    // === Public Properties ===
    public int HealAmount => _healAmount;
}