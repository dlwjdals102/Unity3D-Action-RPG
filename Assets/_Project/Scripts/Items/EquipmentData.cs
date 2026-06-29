using UnityEngine;


/// <summary>
/// 장비 착용 부위. 구조는 7부위 지원, 데이터는 필요한 것부터.
/// </summary>
public enum EquipmentSlot
{
    Weapon, Shield, Helmet, Chest, Legs, Boots, Gloves
}

/// <summary>
/// 장비 아이템 (무기/방패/방어구). ItemData 를 상속.
/// 착용 슬롯(부위)과 스탯 보정(공격력/방어력)을 가진다.
/// 
/// 무기/방패/방어구를 부위별 클래스로 나누지 않고 슬롯(enum)으로 구분한다
/// (스탯 보정 구조가 동일하므로). 부위 추가 = SO 에셋 생성.
/// </summary>
[CreateAssetMenu(fileName = "Equipment", menuName = "Hollow Blade/Items/Equipment")]
public class EquipmentData : ItemData
{
    [Header("Equipment")]
    [Tooltip("착용 부위")]
    [SerializeField] private EquipmentSlot _slot;

    [Tooltip("공격력 보정 (착용 시 합산). 무기는 WeaponDamage를 쓰므로 0 권장 - 방어구/장신구용")]
    [SerializeField] private int _attackBonus = 0;

    [Tooltip("무기 기본 데미지 (무기 슬롯 전용. 콤보 데미지의 기준값)")]
    [SerializeField] private int _weaponDamage = 0;

    [Tooltip("방어력 보정 (착용 시 합산)")]
    [SerializeField] private int _defenseBonus = 0;

    // === Public Properties ===
    public EquipmentSlot Slot => _slot;
    public int AttackBonus => _attackBonus;
    public int WeaponDamage => _weaponDamage;
    public int DefenseBonus => _defenseBonus;
}