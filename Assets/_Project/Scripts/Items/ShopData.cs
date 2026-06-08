using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 상점 판매 목록 (ScriptableObject). 판매할 아이템들을 담는다.
/// 가격은 각 ItemData.Price 를 사용. 상점 추가 = SO 에셋 생성 (데이터 주도).
/// </summary>
[CreateAssetMenu(fileName = "Shop", menuName = "Hollow Blade/Shop")]
public class ShopData : ScriptableObject
{
    [Tooltip("판매 아이템 목록")]
    [SerializeField] private List<ItemData> _itemsForSale = new List<ItemData>();

    /// <summary>판매 목록 (읽기 전용).</summary>
    public IReadOnlyList<ItemData> ItemsForSale => _itemsForSale;
}