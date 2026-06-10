using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 모든 아이템(ItemData)을 ID로 찾을 수 있는 데이터베이스 (ScriptableObject).
/// 세이브/로드 시 JSON에 저장된 ID로 실제 ItemData(SO)를 복원하는 데 사용한다.
/// 
/// 아이템 추가 = 이 목록에 SO 등록 (데이터 주도). ID는 ItemData.Id 사용.
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Hollow Blade/Item Database")]
public class ItemDatabase : ScriptableObject
{
    [Tooltip("게임의 모든 아이템 SO (ID로 조회됨)")]
    [SerializeField] private List<ItemData> _items = new List<ItemData>();

    // ID → ItemData 빠른 조회용 (런타임 구축)
    private Dictionary<string, ItemData> _lookup;

    /// <summary>ID로 아이템을 찾는다 (없으면 null).</summary>
    public ItemData GetById(string id)
    {
        if (string.IsNullOrEmpty(id)) return null;

        // 최초 호출 시 딕셔너리 구축
        if (_lookup == null)
        {
            BuildLookup();
        }

        return _lookup.TryGetValue(id, out var item) ? item : null;
    }

    private void BuildLookup()
    {
        _lookup = new Dictionary<string, ItemData>();
        foreach (var item in _items)
        {
            if (item == null || string.IsNullOrEmpty(item.Id)) continue;

            if (_lookup.ContainsKey(item.Id))
            {
                Debug.LogWarning($"[ItemDatabase] 중복 ID: {item.Id} ({item.name})");
                continue;
            }
            _lookup[item.Id] = item;
        }
    }
}