using UnityEngine;

/// <summary>
/// 적 처치 시 아이템을 드롭합니다.
/// Enemy 오브젝트에 부착합니다.
/// </summary>
public class ItemDropper : MonoBehaviour
{
    [Header("Drop Table")]
    [SerializeField] private DropEntry[] _dropTable;

    [Header("Drop Settings")]
    [SerializeField] private float _dropForce = 3f;
    [SerializeField] private float _dropUpForce = 5f;

    [System.Serializable]
    public struct DropEntry
    {
        public ItemData item;
        [Range(0f, 1f)]
        public float dropChance;
        public int minAmount;
        public int maxAmount;
    }

    /// <summary>아이템을 드롭합니다. 사망 시 호출합니다.</summary>
    public void DropItems()
    {
        if (_dropTable == null) return;

        foreach (var entry in _dropTable)
        {
            if (entry.item == null) continue;
            if (Random.value > entry.dropChance) continue;

            int amount = Random.Range(entry.minAmount, entry.maxAmount + 1);
            SpawnDropItem(entry.item, amount);
        }
    }

    private void SpawnDropItem(ItemData item, int amount)
    {
        // 드롭 프리팹이 있으면 사용, 없으면 기본 Sphere 생성
        GameObject dropObj;

        // 생성 위치에 랜덤 오프셋 (겹침 방지)
        Vector3 randomOffset = new Vector3(
            Random.Range(-0.3f, 0.3f),
            0f,
            Random.Range(-0.3f, 0.3f)
        );
        Vector3 spawnPos = transform.position + Vector3.up + randomOffset;

        if (item.dropPrefab != null)
        {
            dropObj = Instantiate(item.dropPrefab, spawnPos, Quaternion.identity);
        }
        else
        {
            // 기본 드롭 오브젝트 생성
            dropObj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            dropObj.transform.position = spawnPos;
            dropObj.transform.localScale = Vector3.one * 0.3f;

            // 희귀도 색상 적용
            var renderer = dropObj.GetComponent<Renderer>();
            if (renderer != null)
                renderer.material.color = item.GetRarityColor();
        }

        dropObj.name = $"Drop_{item.itemName}";

        // Collider가 없으면 추가 (ItemPickup의 RequireComponent 충족)
        if (dropObj.GetComponent<Collider>() == null)
            dropObj.AddComponent<SphereCollider>();

        // ItemPickup 컴포넌트 추가
        var pickup = dropObj.GetComponent<ItemPickup>();
        if (pickup == null)
            pickup = dropObj.AddComponent<ItemPickup>();
        pickup.Initialize(item, amount);

        // 물리 튀어오르기 효과
        var rb = dropObj.GetComponent<Rigidbody>();
        if (rb == null)
            rb = dropObj.AddComponent<Rigidbody>();

        Vector3 randomDir = new Vector3(
            Random.Range(-1f, 1f),
            0f,
            Random.Range(-1f, 1f)
        ).normalized;

        rb.AddForce(
            randomDir * _dropForce + Vector3.up * _dropUpForce,
            ForceMode.Impulse
        );

        // 일정 시간 후 자동 삭제 (수거 안 하면)
        Destroy(dropObj, 30f);
    }
}