using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// 화면 상단 알림(토스트) 시스템.
/// 아이템 획득, 레벨업 등의 이벤트를 구독하여 자동으로 알림을 표시합니다.
/// 
/// [UI 구조]
/// - NotificationContainer (VerticalLayoutGroup) 필요
/// - Notification Entry Prefab: TextMeshProUGUI + CanvasGroup 포함
/// </summary>
public class NotificationUI : Singleton<NotificationUI>
{
    [Header("Container")]
    [Tooltip("알림이 쌓일 부모 (Vertical Layout Group 권장)")]
    [SerializeField] private Transform _container;

    [Header("Prefab")]
    [Tooltip("알림 한 개의 프리팹 (TextMeshProUGUI + CanvasGroup)")]
    [SerializeField] private GameObject _notificationPrefab;

    [Header("Settings")]
    [SerializeField] private float _displayDuration = 2.5f;
    [SerializeField] private float _fadeInDuration = 0.2f;
    [SerializeField] private float _fadeOutDuration = 0.5f;
    [SerializeField] private int _maxVisible = 5;

    [Header("Colors")]
    [SerializeField] private Color _itemColor = new Color(0.8f, 1f, 0.8f);
    [SerializeField] private Color _levelUpColor = new Color(1f, 0.85f, 0.2f);
    [SerializeField] private Color _infoColor = Color.white;

    // ── 내부 ──
    private readonly Queue<GameObject> _activeNotifications = new Queue<GameObject>();
    private InventorySystem _playerInventory;
    private PlayerStats _playerStats;

    protected override void OnSingletonAwake()
    {
        // Container가 비어있으면 자기 자신을 사용
        if (_container == null)
            _container = transform;
    }

    private void Start()
    {
        // 플레이어 이벤트 구독
        GameObject player = GameObject.FindGameObjectWithTag(Define.Tag.Player);
        if (player != null)
        {
            _playerInventory = player.GetComponent<InventorySystem>();
            _playerStats = player.GetComponent<PlayerStats>();

            if (_playerInventory != null)
                _playerInventory.OnItemAdded += OnItemAdded;

            if (_playerStats != null)
                _playerStats.OnLevelUp += OnLevelUp;
        }
    }

    protected override void OnSingletonDestroy()
    {
        if (_playerInventory != null)
            _playerInventory.OnItemAdded -= OnItemAdded;

        if (_playerStats != null)
            _playerStats.OnLevelUp -= OnLevelUp;
    }

    // ════════════════════════════════════════════════════
    //  이벤트 핸들러
    // ════════════════════════════════════════════════════

    private void OnItemAdded(ItemData item, int amount)
    {
        if (item == null) return;

        string msg = amount > 1
            ? $"{item.itemName} x{amount} 획득"
            : $"{item.itemName} 획득";

        Color color = item.rarity == Define.Rarity.Common ? _itemColor : item.GetRarityColor();
        Show(msg, color);
    }

    private void OnLevelUp(int newLevel)
    {
        Show($"LEVEL UP! Lv.{newLevel}", _levelUpColor);
    }

    // ════════════════════════════════════════════════════
    //  표시
    // ════════════════════════════════════════════════════

    /// <summary>알림을 화면에 표시합니다. 외부에서도 호출 가능.</summary>
    public void Show(string message, Color? color = null)
    {
        if (_notificationPrefab == null || _container == null) return;

        // 최대 개수 초과 시 오래된 것 제거
        while (_activeNotifications.Count >= _maxVisible)
        {
            GameObject oldest = _activeNotifications.Dequeue();
            if (oldest != null) Destroy(oldest);
        }

        GameObject notif = Instantiate(_notificationPrefab, _container);
        _activeNotifications.Enqueue(notif);

        var text = notif.GetComponentInChildren<TextMeshProUGUI>();
        if (text != null)
        {
            text.text = message;
            text.color = color ?? _infoColor;
        }

        StartCoroutine(NotificationRoutine(notif));
    }

    private IEnumerator NotificationRoutine(GameObject notif)
    {
        CanvasGroup cg = notif.GetComponent<CanvasGroup>();
        if (cg == null) cg = notif.AddComponent<CanvasGroup>();

        // 페이드 인
        cg.alpha = 0f;
        float t = 0f;
        while (t < _fadeInDuration)
        {
            t += Time.deltaTime;
            cg.alpha = Mathf.Clamp01(t / _fadeInDuration);
            yield return null;
        }
        cg.alpha = 1f;

        // 표시 유지
        yield return new WaitForSeconds(_displayDuration);

        // 페이드 아웃
        t = 0f;
        while (t < _fadeOutDuration && notif != null)
        {
            t += Time.deltaTime;
            cg.alpha = 1f - Mathf.Clamp01(t / _fadeOutDuration);
            yield return null;
        }

        if (notif != null)
        {
            Destroy(notif);
        }
    }
}