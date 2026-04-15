using UnityEngine;

/// <summary>
/// 개별 적 리스폰 시스템.
/// 미리 맵에 배치한 적에 부착하면, 사망 후 일정 시간 뒤
/// 같은 위치에 다시 나타납니다.
/// 
/// [사용법]
/// 맵에 배치한 각 적 오브젝트에 이 컴포넌트를 추가합니다.
/// 적이 죽으면 비활성화 → 타이머 → 같은 위치에서 부활합니다.
/// </summary>
public class EnemyRespawn : MonoBehaviour
{
    [Header("Respawn Settings")]
    [SerializeField] private float _respawnTime = 15f;
    [SerializeField] private bool _autoRespawn = true;

    // ── 내부 ──
    private Vector3 _spawnPosition;
    private Quaternion _spawnRotation;
    private EnemyController _controller;
    private EnemyAI _ai;
    private bool _isDead = false;
    private float _respawnTimer;

    private void Awake()
    {
        _controller = GetComponent<EnemyController>();
        _ai = GetComponent<EnemyAI>();

        _spawnPosition = transform.position;
        _spawnRotation = transform.rotation;
    }

    private void Start()
    {
        if (_controller != null)
            _controller.OnDeath += OnEnemyDeath;
    }

    private void OnDestroy()
    {
        if (_controller != null)
            _controller.OnDeath -= OnEnemyDeath;
    }

    private void Update()
    {
        if (!_isDead || !_autoRespawn) return;

        _respawnTimer -= Time.deltaTime;

        if (_respawnTimer <= 0f)
        {
            Respawn();
        }
    }

    private void OnEnemyDeath()
    {
        _isDead = true;
        _respawnTimer = _respawnTime;

        // 오브젝트 파괴 대신 비활성화
        // EnemyAI의 Destroy(gameObject, 5f)를 취소해야 함
        CancelInvoke();
        Invoke(nameof(HideEnemy), 3f);
    }

    private void HideEnemy()
    {
        // 렌더러와 콜라이더만 비활성화 (오브젝트는 유지)
        SetVisibility(false);
    }

    private void Respawn()
    {
        _isDead = false;

        // 위치 복원
        transform.position = _spawnPosition;
        transform.rotation = _spawnRotation;

        // 비주얼 복원
        SetVisibility(true);

        // HP 및 상태 복원
        if (_controller != null)
            _controller.ResetEnemy();

        // NavMeshAgent 위치 동기화
        var agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
        if (agent != null && agent.enabled)
            agent.Warp(_spawnPosition);

        // AI 상태 리셋 (Die → Idle)
        if (_ai != null)
            _ai.ResetAI();

        // 전체 오브젝트 재활성화
        gameObject.SetActive(true);

        Debug.Log($"[EnemyRespawn] {gameObject.name} 리스폰!");
    }

    private void SetVisibility(bool visible)
    {
        // 렌더러 ON/OFF
        var renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers)
            r.enabled = visible;

        // 콜라이더 ON/OFF
        var colliders = GetComponentsInChildren<Collider>();
        foreach (var c in colliders)
            c.enabled = visible;
    }
}