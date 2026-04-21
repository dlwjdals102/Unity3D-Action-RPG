using UnityEngine;

/// <summary>
/// 히트 VFX + 카메라 셰이크 관리자.
/// 씬의 HitBox들의 OnHit 이벤트를 자동 구독하여 이펙트를 생성합니다.
/// 
/// [사용법]
/// 씬에 하나 배치. HitEffect Prefab 연결 필수.
/// Impulse Source는 Player에 붙어있으면 자동 탐색.
/// </summary>
public class HitEffectSpawner : Singleton<HitEffectSpawner>
{
    [Header("VFX")]
    [Tooltip("플레이어가 적 타격 시 재생할 파티클 프리팹")]
    [SerializeField] private GameObject _playerHitVfxPrefab;
    [Tooltip("플레이어 피격 시 재생할 파티클 프리팹")]
    [SerializeField] private GameObject _playerHurtVfxPrefab;
    [Tooltip("VFX 자동 제거 시간")]
    [SerializeField] private float _vfxLifetime = 2f;

    [Header("Camera Shake")]
    [Tooltip("Impulse Source 컴포넌트 (Cinemachine). 비워두면 씬에서 자동 탐색")]
    [SerializeField] private MonoBehaviour _impulseSource;
    [Tooltip("일반 공격 셰이크 강도")]
    [SerializeField] private float _normalShakeForce = 0.3f;
    [Tooltip("스킬/크리티컬 셰이크 강도")]
    [SerializeField] private float _criticalShakeForce = 0.8f;

    // ── 내부 ──
    private HitBox[] _trackedHitBoxes;

    protected override void OnSingletonAwake()
    {
        // Impulse Source 자동 탐색
        if (_impulseSource == null)
            FindImpulseSource();
    }

    private void Start()
    {
        // 모든 HitBox 수집 및 이벤트 구독
        SubscribeToAllHitBoxes();
    }

    protected override void OnSingletonDestroy()
    {
        UnsubscribeFromAllHitBoxes();
    }

    // ════════════════════════════════════════════════════
    //  HitBox 구독 관리
    // ════════════════════════════════════════════════════

    private void SubscribeToAllHitBoxes()
    {
        _trackedHitBoxes = FindObjectsByType<HitBox>(FindObjectsInactive.Include, FindObjectsSortMode.None);

        foreach (var hitbox in _trackedHitBoxes)
        {
            if (hitbox != null)
                hitbox.OnHit += OnHitDetected;
        }

        Debug.Log($"[HitEffectSpawner] {_trackedHitBoxes.Length}개의 HitBox 구독");
    }

    private void UnsubscribeFromAllHitBoxes()
    {
        if (_trackedHitBoxes == null) return;

        foreach (var hitbox in _trackedHitBoxes)
        {
            if (hitbox != null)
                hitbox.OnHit -= OnHitDetected;
        }
    }

    /// <summary>새로 생성된 HitBox를 수동으로 구독합니다 (적 리스폰 시 등).</summary>
    public void SubscribeHitBox(HitBox hitbox)
    {
        if (hitbox == null) return;
        hitbox.OnHit -= OnHitDetected;
        hitbox.OnHit += OnHitDetected;
    }

    // ════════════════════════════════════════════════════
    //  이벤트 핸들러
    // ════════════════════════════════════════════════════

    private void OnHitDetected(DamageData data, GameObject target)
    {
        if (target == null) return;

        // 대상이 플레이어인지 확인
        bool targetIsPlayer = target.transform.root.CompareTag(Define.Tag.Player);

        // VFX 선택
        GameObject vfxPrefab = targetIsPlayer ? _playerHurtVfxPrefab : _playerHitVfxPrefab;
        if (vfxPrefab != null)
        {
            GameObject vfx = Instantiate(vfxPrefab, data.HitPoint, Quaternion.identity);
            Destroy(vfx, _vfxLifetime);
        }

        // 카메라 셰이크 (Cinemachine Impulse)
        float shakeForce = _normalShakeForce;
        TriggerImpulse(shakeForce);
    }

    /// <summary>외부에서 호출 가능한 카메라 셰이크. 스킬 등에서 강하게 흔들 때 사용.</summary>
    public void TriggerStrongShake()
    {
        TriggerImpulse(_criticalShakeForce);
    }

    // ════════════════════════════════════════════════════
    //  Cinemachine Impulse 호출 (리플렉션)
    //  Cinemachine 직접 참조를 피하기 위한 방식
    // ════════════════════════════════════════════════════

    private void TriggerImpulse(float force)
    {
        if (_impulseSource == null) return;

        // CinemachineImpulseSource.GenerateImpulse(float force) 호출
        var method = _impulseSource.GetType().GetMethod(
            "GenerateImpulse",
            new System.Type[] { typeof(float) }
        );

        if (method != null)
            method.Invoke(_impulseSource, new object[] { force });
    }

    private void FindImpulseSource()
    {
        // 씬에서 CinemachineImpulseSource 컴포넌트 탐색
        foreach (var comp in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (comp.GetType().Name == "CinemachineImpulseSource")
            {
                _impulseSource = comp;
                Debug.Log($"[HitEffectSpawner] Impulse Source 자동 탐색: {comp.gameObject.name}");
                return;
            }
        }

        Debug.LogWarning("[HitEffectSpawner] CinemachineImpulseSource를 찾을 수 없습니다.");
    }
}