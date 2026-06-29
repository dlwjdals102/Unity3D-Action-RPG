using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 데미지 텍스트 풀 관리 + Spawn API 제공 Singleton.
/// 초기 N개 미리 생성 (풀링), 부족 시 추가 Instantiate (Soft Cap).
/// 단일 책임: 데미지 텍스트의 생명주기 관리.
/// </summary>
public class DamageTextManager : MonoBehaviour
{
    public static DamageTextManager Instance { get; private set; }

    [Header("Pool")]
    [SerializeField] private DamageText _damageTextPrefab;
    [SerializeField] private int _initialPoolSize = 10;

    private Queue<DamageText> _pool = new Queue<DamageText>();

    private void Awake()
    {
        // Singleton 셋업
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[DamageTextManager] Duplicate instance found, destroying.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializePool();
    }

    private void InitializePool()
    {
        if (_damageTextPrefab == null)
        {
            Debug.LogError("[DamageTextManager] DamageText prefab not assigned!");
            return;
        }

        for (int i = 0; i < _initialPoolSize; i++)
        {
            DamageText text = Instantiate(_damageTextPrefab, transform);
            text.gameObject.SetActive(false);
            _pool.Enqueue(text);
        }
    }

    // ========================================================================
    // === Public API ===
    // ========================================================================

    /// <summary>
    /// 데미지 텍스트 생성. 외부 (TargetDummy, PlayerHealth 등) 가 호출.
    /// 풀에서 재사용 또는 부족 시 추가 생성.
    /// </summary>
    public void Spawn(int damage, Vector3 worldPosition)
    {
        DamageText text = GetFromPool();
        text.Initialize(damage, worldPosition);
    }

    /// <summary>문자열 텍스트(Guard/Parry 등)를 지정 색으로 띄운다.</summary>
    public void SpawnText(string text, Vector3 worldPosition, Color color)
    {
        DamageText text2 = GetFromPool();
        text2.Initialize(text, color, worldPosition);
    }

    /// <summary>
    /// 데미지 텍스트가 수명 종료 시 호출. 풀에 반환.
    /// </summary>
    public void ReturnToPool(DamageText text)
    {
        if (text == null) return;

        text.gameObject.SetActive(false);
        _pool.Enqueue(text);
    }

    // ========================================================================
    // === Internal Helpers ===
    // ========================================================================

    private DamageText GetFromPool()
    {
        if (_pool.Count > 0)
        {
            return _pool.Dequeue();
        }

        // 풀 비었으면 추가 생성 (Soft Cap)
        Debug.LogWarning("[DamageTextManager] Pool exhausted, creating extra instance.");
        return Instantiate(_damageTextPrefab, transform);
    }
}