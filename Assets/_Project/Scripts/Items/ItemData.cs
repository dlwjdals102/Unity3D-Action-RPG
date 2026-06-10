using UnityEngine;

/// <summary>
/// 모든 아이템의 공통 데이터 (ScriptableObject 베이스).
/// 무기/방패/방어구(EquipmentData), 소비 아이템(ConsumableData) 등이 이를 상속한다.
/// 아이템 추가 = SO 에셋 생성 (코드 수정 없이 데이터로 확장).
/// 
/// 베이스 자체는 에셋으로 만들지 않으므로 CreateAssetMenu 를 두지 않는다 (파생만 생성).
/// </summary>
public abstract class ItemData : ScriptableObject
{
    [Header("Identity")]
    [Tooltip("고유 ID (세이브/로드 키). 아이템마다 유일해야 하며, 한번 정하면 바꾸지 않는다.")]
    [SerializeField] private string _id;

    [Header("Common")]
    [Tooltip("아이템 표시 이름")]
    [SerializeField] private string _displayName = "Item";

    [Tooltip("인벤토리 아이콘")]
    [SerializeField] private Sprite _icon;

    [Tooltip("설명 (인벤토리/상점 표시)")]
    [TextArea]
    [SerializeField] private string _description;

    [Tooltip("상점 기본 가격 (돈/영혼)")]
    [SerializeField] private int _price = 0;

    [Tooltip("한 칸에 쌓을 수 있는 최대 개수 (1 = 중첩 불가, 무기/방어구는 보통 1)")]
    [SerializeField] private int _maxStack = 1;

    // === Public Properties ===
    public string Id => _id;
    public string DisplayName => _displayName;
    public Sprite Icon => _icon;
    public string Description => _description;
    public int Price => _price;
    public int MaxStack => _maxStack;
}