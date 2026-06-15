using UnityEngine;

/// <summary>
/// 영혼 떨구기/회수 규칙 관리 (씬에 하나).
/// - 사망 시: 보유 영혼 전액을 사망 위치에 드롭(SoulDrop 생성)하고 보유는 0
/// - 월드에 드롭은 항상 1개만: 회수 전에 또 죽으면 기존 드롭은 영구 소멸
/// - 세이브 연동([S-3])을 위해 드롭 정보 노출/복원 API 제공
/// </summary>
public class SoulDropManager : MonoBehaviour
{
    public static SoulDropManager Instance { get; private set; }

    [SerializeField] private SoulDrop _dropPrefab;
    [SerializeField] private PlayerHealth _playerHealth;
    [SerializeField] private PlayerSouls _playerSouls;

    [Tooltip("드롭 생성 높이 보정 (바닥에 파묻히지 않게)")]
    [SerializeField] private float _spawnHeightOffset = 0.5f;

    private SoulDrop _currentDrop;

    // === 세이브용 정보 ([S-3]에서 사용) ===
    public bool HasDrop => _currentDrop != null;
    public Vector3 DropPosition => _currentDrop != null ? _currentDrop.transform.position : Vector3.zero;
    public int DropAmount => _currentDrop != null ? _currentDrop.Amount : 0;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnEnable()
    {
        if (_playerHealth != null) _playerHealth.OnDeath += HandleDeath;
    }

    private void OnDisable()
    {
        if (_playerHealth != null) _playerHealth.OnDeath -= HandleDeath;
    }

    private void HandleDeath()
    {
        // 1개 규칙: 회수 못 한 기존 드롭은 영구 소멸
        ClearDrop();

        // 빈손 사망이면 드롭 없음
        if (_playerSouls == null || _playerSouls.Souls <= 0) return;

        // 사망 위치에 전액 드롭 + 보유 비우기
        SpawnDrop(_playerHealth.transform.position + Vector3.up * _spawnHeightOffset,
                  _playerSouls.Souls);
        _playerSouls.SetSouls(0);
    }

    /// <summary>드롭 생성 (사망/세이브 복원 공용).</summary>
    public void SpawnDrop(Vector3 position, int amount)
    {
        if (_dropPrefab == null || amount <= 0) return;

        _currentDrop = Instantiate(_dropPrefab, position, Quaternion.identity);
        _currentDrop.SetAmount(amount);
    }

    /// <summary>드롭이 회수됨 (SoulDrop 이 호출 - 참조 정리).</summary>
    public void NotifyRecovered(SoulDrop drop)
    {
        if (_currentDrop == drop) _currentDrop = null;
    }

    /// <summary>현재 드롭 제거 (1개 규칙 / 로드 전 초기화 공용).</summary>
    public void ClearDrop()
    {
        if (_currentDrop != null)
        {
            Destroy(_currentDrop.gameObject);
            _currentDrop = null;
        }
    }
}