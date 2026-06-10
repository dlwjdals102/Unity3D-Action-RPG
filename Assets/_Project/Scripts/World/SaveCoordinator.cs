using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// 세이브/로드 조율자. 각 시스템(인벤토리/장비/영혼/체크포인트)의 데이터를
/// SaveData로 모아 저장하고, 로드 시 SaveData를 각 시스템에 적용한다.
/// 
/// SaveManager(파일 I/O)와 각 시스템 사이의 조율 담당.
/// SO 참조 ↔ ID 변환은 ItemDatabase 를 통해 처리.
/// </summary>
public class SaveCoordinator : MonoBehaviour
{
    [SerializeField] private Inventory _inventory;
    [SerializeField] private EquipmentManager _equipment;
    [SerializeField] private PlayerSouls _souls;
    [SerializeField] private ItemDatabase _itemDatabase;
    [SerializeField] private PlayerRespawn _playerRespawn;

    private void Start()
    {
        // 게임 시작 시 저장 파일이 있으면 자동 로드 (없으면 새 게임)
        if (SaveManager.Instance != null && SaveManager.Instance.HasSave())
        {
            LoadGame();
        }
    }

    /// <summary>현재 게임 상태를 SaveData 로 모아 저장.</summary>
    public void SaveGame()
    {
        var data = new SaveData();

        // 영혼
        if (_souls != null) data.souls = _souls.Souls;

        // 인벤토리 (ItemData → ID)
        if (_inventory != null)
        {
            foreach (var slot in _inventory.Slots)
            {
                if (slot.Item == null) continue;
                data.inventory.Add(new SavedItem
                {
                    itemId = slot.Item.Id,
                    count = slot.Count
                });
            }
        }

        // 장비 (각 부위 → ID)
        if (_equipment != null)
        {
            foreach (EquipmentSlot slot in System.Enum.GetValues(typeof(EquipmentSlot)))
            {
                var item = _equipment.GetEquipped(slot);
                if (item == null) continue;
                data.equipment.Add(new SavedEquipment
                {
                    slot = slot.ToString(),
                    itemId = item.Id
                });
            }
        }

        // 체크포인트
        if (BonfireManager.Instance != null && BonfireManager.Instance.HasCheckpoint)
        {
            Vector3 pos = BonfireManager.Instance.CheckpointPosition;
            data.hasCheckpoint = true;
            data.checkpointX = pos.x;
            data.checkpointY = pos.y;
            data.checkpointZ = pos.z;
            data.checkpointRotY = BonfireManager.Instance.CheckpointRotation.eulerAngles.y;
        }

        // 파일 저장
        if (SaveManager.Instance != null) SaveManager.Instance.Save(data);
    }

    /// <summary>저장 파일을 불러와 각 시스템에 적용.</summary>
    public void LoadGame()
    {
        if (SaveManager.Instance == null) return;

        SaveData data = SaveManager.Instance.Load();
        if (data == null) return;  // 저장 없음 (새 게임)

        // 영혼
        if (_souls != null) _souls.SetSouls(data.souls);

        // 인벤토리 복원 (비우고 → ID로 아이템 복원)
        if (_inventory != null && _itemDatabase != null)
        {
            _inventory.Clear();
            foreach (var saved in data.inventory)
            {
                var item = _itemDatabase.GetById(saved.itemId);
                if (item != null) _inventory.AddItem(item, saved.count);
            }
        }

        // 장비 복원 (비우고 → ID로 착용)
        if (_equipment != null && _itemDatabase != null)
        {
            _equipment.ClearAll();
            foreach (var saved in data.equipment)
            {
                var item = _itemDatabase.GetById(saved.itemId);
                if (item is EquipmentData equipment)
                {
                    _equipment.SetEquippedDirect(equipment);
                }
            }
        }

        // 체크포인트
        if (data.hasCheckpoint && BonfireManager.Instance != null)
        {
            Vector3 pos = new Vector3(data.checkpointX, data.checkpointY, data.checkpointZ);
            Quaternion rot = Quaternion.Euler(0f, data.checkpointRotY, 0f);
            BonfireManager.Instance.SetCheckpoint(pos, rot);

            // 로드 시 플레이어를 마지막 화톳불(체크포인트)에서 시작 (소울라이크 정석)
            if (_playerRespawn != null)
            {
                _playerRespawn.TeleportTo(pos, rot);
            }
        }

        Debug.Log("[SaveCoordinator] 로드 적용 완료");
    }
}