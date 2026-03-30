using UnityEngine;

/// <summary>
/// 제네릭 싱글톤 베이스 클래스.
/// MonoBehaviour를 상속하며, DontDestroyOnLoad 옵션을 제공합니다.
/// 
/// [사용법]
/// public class GameManager : Singleton<GameManager> { }
/// </summary>
public abstract class Singleton<T> : MonoBehaviour where T : MonoBehaviour
{
    // ── 설정 ──────────────────────────────────────────
    [Header("Singleton Settings")]
    [SerializeField] private bool _dontDestroyOnLoad = true;

    // ── 프로퍼티 ──────────────────────────────────────
    private static T _instance;
    private static readonly object _lock = new object();
    private static bool _isQuitting = false;

    public static T Instance
    {
        get
        {
            if (_isQuitting)
            {
                Debug.LogWarning(
                    $"[Singleton] '{typeof(T).Name}' 인스턴스가 이미 파괴되었습니다. " +
                    "OnDestroy 이후 접근을 피해주세요.");
                return null;
            }

            lock (_lock)
            {
                if (_instance == null)
                {
                    // 씬에서 기존 인스턴스 탐색
                    _instance = FindAnyObjectByType<T>();

                    if (_instance == null)
                    {
                        Debug.LogError(
                            $"[Singleton] '{typeof(T).Name}' 인스턴스를 찾을 수 없습니다. " +
                            "씬에 해당 컴포넌트가 부착된 GameObject가 필요합니다.");
                    }
                }
                return _instance;
            }
        }
    }

    /// <summary>
    /// 인스턴스 존재 여부를 안전하게 확인합니다.
    /// OnDestroy에서 다른 싱글톤에 접근할 때 사용합니다.
    /// </summary>
    public static bool HasInstance => _instance != null && !_isQuitting;

    // ── 라이프사이클 ──────────────────────────────────
    protected virtual void Awake()
    {
        if (_instance != null && _instance != this as T)
        {
            Debug.LogWarning(
                $"[Singleton] '{typeof(T).Name}' 중복 인스턴스 감지 → 파괴합니다.");
            Destroy(gameObject);
            return;
        }

        _instance = this as T;

        if (_dontDestroyOnLoad)
        {
            // 루트 GameObject만 DontDestroyOnLoad 가능
            if (transform.parent != null)
                transform.SetParent(null);

            DontDestroyOnLoad(gameObject);
        }

        OnSingletonAwake();
    }

    protected virtual void OnDestroy()
    {
        if (_instance == this as T)
        {
            _isQuitting = true;
            OnSingletonDestroy();
            _instance = null;
        }
    }

    protected virtual void OnApplicationQuit()
    {
        _isQuitting = true;
    }

    // ── 서브클래스 오버라이드 포인트 ──────────────────
    /// <summary>
    /// Awake 시점에서 싱글톤 초기화가 완료된 후 호출됩니다.
    /// 기존 Awake() 대신 이 메서드를 오버라이드하세요.
    /// </summary>
    protected virtual void OnSingletonAwake() { }

    /// <summary>
    /// 싱글톤이 파괴될 때 호출됩니다.
    /// 기존 OnDestroy() 대신 이 메서드를 오버라이드하세요.
    /// </summary>
    protected virtual void OnSingletonDestroy() { }
}