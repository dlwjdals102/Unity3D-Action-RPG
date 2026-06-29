using UnityEngine;

/// <summary>
/// 화톳불. 플레이어가 트리거 범위 안에서 상호작용(F)하면 휴식한다.
/// 휴식 시 이 화톳불 위치를 체크포인트로 등록 (BonfireManager).
/// 
/// 트리거 범위 진입/이탈을 OnTriggerEnter/Exit 로 감지하고, 범위 안일 때만 F 입력을 처리한다.
/// (회복/적 리스폰은 휴식 시 처리)
/// 
/// 필요: 이 오브젝트에 Trigger 로 설정된 Collider, 플레이어에 "Player" 태그.
/// </summary>
public class Bonfire : MonoBehaviour
{
    [Header("Interaction")]
    [Tooltip("상호작용 가능 거리 표시용 (실제 감지는 Trigger Collider)")]
    [SerializeField] private Transform _restPoint;  // 휴식 시 리스폰될 지점 (비우면 자기 위치)

    [Tooltip("범위 진입 시 표시할 월드 프롬프트 ([F] 휴식 라벨)")]
    [SerializeField] private GameObject _promptObject;

    private PlayerController _playerInRange;  // 범위 안 플레이어 (없으면 null)

    private SaveCoordinator _saveCoordinator;

    private void Awake()
    {
        // 세이브 조율자 캐싱 (씬에 하나)
        _saveCoordinator = FindFirstObjectByType<SaveCoordinator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // 플레이어가 범위에 진입
        var controller = other.GetComponentInParent<PlayerController>();
        if (controller != null)
        {
            _playerInRange = controller;
            if (_promptObject != null) _promptObject.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var controller = other.GetComponentInParent<PlayerController>();
        if (controller != null && controller == _playerInRange)
        {
            _playerInRange = null;
            if (_promptObject != null) _promptObject.SetActive(false);
        }
    }

    private void Update()
    {
        // 범위 안 + F 입력 → 휴식
        if (_playerInRange == null) return;

        if (_playerInRange.InteractRequested)
        {
            // 전투 중에는 휴식 불가 (소울라이크: 안전할 때만 정비)
            if (EnemyRespawnManager.Instance != null &&
                EnemyRespawnManager.Instance.AnyEnemyInCombat())
            {
                return;
            }

            Rest();
        }
    }

    /// <summary>휴식: 체크포인트 등록.</summary>
    private void Rest()
    {
        Vector3 restPos = _restPoint != null ? _restPoint.position : transform.position;
        Quaternion restRot = _restPoint != null ? _restPoint.rotation : transform.rotation;

        if (BonfireManager.Instance != null)
        {
            BonfireManager.Instance.SetCheckpoint(restPos, restRot);
        }

        // 휴식 시 체력 완전 회복
        var health = _playerInRange.GetComponent<PlayerHealth>();
        if (health != null) health.ResetHealth();

        // 적 전체 리스폰 (소울라이크: 휴식하면 잡몹 부활)
        if (EnemyRespawnManager.Instance != null)
        {
            EnemyRespawnManager.Instance.RespawnAll();
        }

        // 진행 저장 (휴식 = 저장, 소울라이크 정석. 체크포인트/영혼/인벤토리/장비)
        if (_saveCoordinator != null)
        {
            _saveCoordinator.SaveGame();
        }
    }
}