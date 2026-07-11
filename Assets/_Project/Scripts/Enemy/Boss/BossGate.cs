using UnityEngine;

/// <summary>
/// 보스 아레나 관리. 입구 안개 통과 → 보스 기상 + 봉인 + 체력바.
/// 보스 클리어 시: 입구 안개 소멸, 출구 개방, 세이브포인트 활성화, 즉시 저장.
/// 재시작 시 이미 잡은 보스는 클리어 상태로 시작.
/// </summary>
public class BossGate : MonoBehaviour
{
    private enum GateState { Idle, Engaged, Cleared }

    [Header("Boss")]
    [SerializeField] private BossStateMachine _boss;
    [SerializeField] private EnemyHealth _bossHealth;
    [SerializeField] private string _bossName = "";
    [Tooltip("저장/식별용 고유 ID (보스마다 다르게)")]
    [SerializeField] private string _bossId = "";

    [Header("입구 봉인 / 안개")]
    [Tooltip("전투 중 입구를 막는 봉인 콜라이더 (기본 비활성)")]
    [SerializeField] private Collider _sealCollider;
    [Tooltip("입구 안개 비주얼 (보스 사망 시 사라짐). 시작 시 활성 상태로 둘 것")]
    [SerializeField] private GameObject _fogVisual;

    [Header("출구 봉인 / 안개 (클리어 시 개방)")]
    [Tooltip("클리어 전 출구를 막는 봉인 콜라이더")]
    [SerializeField] private Collider _exitSeal;
    [Tooltip("출구 안개 비주얼 (클리어 시 사라짐)")]
    [SerializeField] private GameObject _exitFog;

    [Header("세이브포인트 (클리어 시 활성화)")]
    [Tooltip("보스방에 비활성 상태로 배치된 화톳불. 클리어 시 활성화됨")]
    [SerializeField] private GameObject _bonfire;

    [Header("UI")]
    [SerializeField] private BossHealthBar _healthBar;

    private GateState _state = GateState.Idle;
    private PlayerHealth _playerHealth;
    private SaveCoordinator _saveCoordinator;

    private void Awake()
    {
        if (_sealCollider != null) _sealCollider.enabled = false;   // 입구: 시작은 통과 가능
        if (_exitSeal != null) _exitSeal.enabled = true;            // 출구: 시작은 막힘
        if (_bonfire != null) _bonfire.SetActive(false);           // 세이브포인트: 시작은 비활성

        // 참조 찾기 (구독은 OnEnable에서)
        _playerHealth = FindFirstObjectByType<PlayerHealth>();
        _saveCoordinator = FindFirstObjectByType<SaveCoordinator>();
    }

    private void OnEnable()
    {
        if (_playerHealth != null) _playerHealth.OnDeath += HandlePlayerDeath;
        if (_bossHealth != null) _bossHealth.OnDeath += HandleBossDeath;
        if (_saveCoordinator != null) _saveCoordinator.OnLoadComplete += RefreshClearedState;
    }

    private void OnDisable()
    {
        if (_playerHealth != null) _playerHealth.OnDeath -= HandlePlayerDeath;
        if (_bossHealth != null) _bossHealth.OnDeath -= HandleBossDeath;
        if (_saveCoordinator != null) _saveCoordinator.OnLoadComplete -= RefreshClearedState;
    }

    private void Start()
    {
        // 로드가 이미 끝난 경우 즉시 확인 (아직이면 OnLoadComplete 구독으로 처리됨)
        if (_saveCoordinator != null && _saveCoordinator.LoadCompleted)
        {
            RefreshClearedState();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (_state != GateState.Idle) return;
        if (other.GetComponentInParent<PlayerHealth>() == null) return;   // 플레이어만
        Engage();
    }

    // === 전투 시작 ===
    private void Engage()
    {
        _state = GateState.Engaged;
        if (_sealCollider != null) _sealCollider.enabled = true;          // 입구 봉인 (도망 차단)
        _boss?.Activate();                                                 // 보스 기상
        if (_healthBar != null && _bossHealth != null)
            _healthBar.Show(_bossHealth, _bossName);                       // 체력바 등장
    }

    // === 보스 사망: 클리어 처리 + 즉시 저장 ===
    private void HandleBossDeath()
    {
        _state = GateState.Cleared;
        OpenArena();                                                       // 입구/출구/세이브포인트 처리
        if (_healthBar != null) _healthBar.Hide();

        // 클리어 등록 + 즉시 저장 (재시작해도 부활 안 함)
        if (_saveCoordinator != null) _saveCoordinator.MarkBossDefeated(_bossId);
    }

    // === 플레이어 전투 중 사망: 재도전 준비 ===
    private void HandlePlayerDeath()
    {
        if (_state != GateState.Engaged) return;   // 전투 중 사망만 처리
        _boss?.ResetToInitial();                   // 보스 휴면/풀피/시작위치 복원
        if (_sealCollider != null) _sealCollider.enabled = false;   // 입구 봉인 해제 → 재진입 가능
        if (_healthBar != null) _healthBar.Hide();
        _state = GateState.Idle;                   // 재진입하면 다시 Engage
    }

    // === 이미 클리어된 보스: 시작부터 클리어 상태로 (재시작 시) ===
    private void ApplyClearedState()
    {
        _state = GateState.Cleared;
        OpenArena();                                            // 출구 개방·세이브포인트 활성화
        if (_boss != null) _boss.gameObject.SetActive(false);   // 보스 비활성화 (이미 잡음)
    }

    /// <summary>
    /// 로드 완료 시 SaveCoordinator 가 호출. 이미 클리어된 보스면 클리어 상태로 전환.
    /// </summary>
    private void RefreshClearedState()
    {
        if (_saveCoordinator != null && _saveCoordinator.IsBossDefeated(_bossId))
        {
            ApplyClearedState();
        }
    }

    // === 클리어 시 아레나 개방: 입구/출구 안개 소멸, 세이브포인트 활성화 ===
    private void OpenArena()
    {
        if (_sealCollider != null) _sealCollider.enabled = false;   // 입구 봉인 해제
        if (_fogVisual != null) _fogVisual.SetActive(false);        // 입구 안개 소멸
        if (_exitSeal != null) _exitSeal.enabled = false;           // 출구 개방
        if (_exitFog != null) _exitFog.SetActive(false);            // 출구 안개 소멸

        if (_bonfire != null)
        {
            _bonfire.SetActive(true);                               // 세이브포인트 활성화
            // 체크포인트 등록은 화톳불에 위임 (스폰 위치 _restPoint 는 화톳불이 앎)
            var bonfire = _bonfire.GetComponent<Bonfire>();
            if (bonfire != null) bonfire.RegisterAsCheckpoint();
        }
    }
}