using UnityEngine;

/// <summary>
/// ScriptableObject 기반 아이템 데이터 정의.
/// 모든 아이템(무기, 방어구, 소비, 악세서리)을 이 에셋으로 정의합니다.
/// 
/// [사용법]
/// Project 창 → 우클릭 → Create → DarkBlade → Item Data
/// </summary>
[CreateAssetMenu(fileName = "NewItem", menuName = "DarkBlade/Item Data")]
public class ItemData : ScriptableObject
{
    // ════════════════════════════════════════════════════
    //  기본 정보
    // ════════════════════════════════════════════════════

    [Header("Basic Info")]
    public string itemName = "New Item";

    [TextArea(2, 4)]
    public string description = "";

    public Sprite icon;
    public Define.ItemType itemType = Define.ItemType.Consumable;
    public Define.Rarity rarity = Define.Rarity.Common;

    [Tooltip("최대 중첩 수 (1이면 중첩 불가)")]
    public int maxStack = 1;

    // ════════════════════════════════════════════════════
    //  장비 스탯 (무기/방어구/악세서리)
    // ════════════════════════════════════════════════════

    [Header("Equipment Stats (장비 전용)")]
    public float bonusAttack = 0f;
    public float bonusDefense = 0f;
    public float bonusMaxHp = 0f;

    [Header("Weapon HitBox (무기 전용)")]
    [Tooltip("무기 장착 시 히트박스 크기 (BoxCollider)")]
    public Vector3 weaponHitBoxSize = new Vector3(0.5f, 0.5f, 1.2f);
    [Tooltip("무기 장착 시 히트박스 중심 오프셋")]
    public Vector3 weaponHitBoxCenter = new Vector3(0f, 0f, 0.6f);

    // ════════════════════════════════════════════════════
    //  소비 아이템 효과
    // ════════════════════════════════════════════════════

    [Header("Consumable Effect (소비 아이템 전용)")]
    public ConsumableType consumableType = ConsumableType.None;
    public float effectAmount = 0f;

    // ════════════════════════════════════════════════════
    //  드롭 설정
    // ════════════════════════════════════════════════════

    [Header("Drop Settings")]
    [Tooltip("월드에 떨어질 때 사용할 프리팹")]
    public GameObject dropPrefab;

    // ════════════════════════════════════════════════════
    //  헬퍼
    // ════════════════════════════════════════════════════

    /// <summary>장비 아이템인지 여부</summary>
    public bool IsEquipment =>
        itemType == Define.ItemType.Weapon ||
        itemType == Define.ItemType.Armor ||
        itemType == Define.ItemType.Accessory;

    /// <summary>소비 아이템인지 여부</summary>
    public bool IsConsumable => itemType == Define.ItemType.Consumable;

    /// <summary>중첩 가능한지 여부</summary>
    public bool IsStackable => maxStack > 1;

    /// <summary>희귀도에 따른 색상 반환 (UI용)</summary>
    public Color GetRarityColor()
    {
        return rarity switch
        {
            Define.Rarity.Common => Color.white,
            Define.Rarity.Uncommon => new Color(0.2f, 0.8f, 0.2f),
            Define.Rarity.Rare => new Color(0.2f, 0.4f, 1f),
            Define.Rarity.Epic => new Color(0.6f, 0.2f, 0.8f),
            Define.Rarity.Legendary => new Color(1f, 0.6f, 0f),
            _ => Color.white
        };
    }
}

/// <summary>소비 아이템 효과 타입</summary>
public enum ConsumableType
{
    None,
    HealHp,
}