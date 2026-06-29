using UnityEngine;

/// <summary>
/// 보스 아레나 안개벽 게이트. 플레이어 통과 → 보스 기상 + 봉인 + 체력바.
/// 생명주기: Idle → (통과) Engaged → (보스 사망) Cleared / (플레이어 사망) Idle 재도전.
/// 소울식: 죽으면 보스 풀피 리셋 후 안개 재통과로 재도전, 잡으면 안개 소멸.
/// </summary>
public class BossGate : MonoBehaviour
{
    private enum GateState { Idle, Engaged, Cleared }

    [Header("Boss")]
    [SerializeField] private BossStateMachine _boss;
    [SerializeField] private EnemyHealth _bossHealth;
    [SerializeField] private string _bossName = "Hollow Blade";

    [Header("Barrier")]
    [Tooltip("전투 중 출구를 막는 봉인 콜라이더 (기본 비활성)")]
    [SerializeField] private Collider _sealCollider;
    [Tooltip("안개 비주얼 (보스 사망 시 사라짐). 시작 시 활성 상태로 둘 것")]
    [SerializeField] private GameObject _fogVisual;

    [Header("UI")]
    [SerializeField] private BossHealthBar _healthBar;

    private GateState _state = GateState.Idle;
    private PlayerHealth _playerHealth;

    private void Awake()
    {
        if (_sealCollider != null) _sealCollider.enabled = false;   // 시작: 통과 가능
        // 안개(_fogVisual)는 에디터에서 활성 상태로 두면 됨 (시작부터 보임)
    }

    private void Start()
    {
        _playerHealth = FindFirstObjectByType<PlayerHealth>();
        if (_playerHealth != null) _playerHealth.OnDeath += HandlePlayerDeath;
        if (_bossHealth != null) _bossHealth.OnDeath += HandleBossDeath;
    }

    private void OnDestroy()
    {
        if (_playerHealth != null) _playerHealth.OnDeath -= HandlePlayerDeath;
        if (_bossHealth != null) _bossHealth.OnDeath -= HandleBossDeath;
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
        if (_sealCollider != null) _sealCollider.enabled = true;          // 봉인 (도망 차단)
        _boss?.Activate();                                                 // 보스 기상
        if (_healthBar != null && _bossHealth != null)
            _healthBar.Show(_bossHealth, _bossName);                       // 체력바 등장
    }

    // === 보스 사망: 클리어 ===
    private void HandleBossDeath()
    {
        _state = GateState.Cleared;
        if (_sealCollider != null) _sealCollider.enabled = false;          // 출구 개방
        if (_fogVisual != null) _fogVisual.SetActive(false);               // 안개 소멸
        if (_healthBar != null) _healthBar.Hide();
        // 보스는 죽은 채 유지 (RespawnAll 제외 - 파일 3)
    }

    // === 플레이어 전투 중 사망: 재도전 준비 ===
    private void HandlePlayerDeath()
    {
        if (_state != GateState.Engaged) return;   // 전투 중 사망만 처리(클리어 후 사망은 무시)
        _boss?.ResetToInitial();                   // 보스 휴면/풀피/시작위치 복원
        if (_sealCollider != null) _sealCollider.enabled = false;   // 봉인 해제 → 재진입 가능
        if (_healthBar != null) _healthBar.Hide();
        _state = GateState.Idle;                   // 재진입하면 다시 Engage
    }
}