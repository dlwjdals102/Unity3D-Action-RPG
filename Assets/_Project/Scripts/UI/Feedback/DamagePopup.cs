using TMPro;
using UnityEngine;

/// <summary>
/// 데미지 숫자 팝업 (개별 인스턴스).
/// 생성되면 위로 떠오르며 페이드 아웃 후 스스로 제거.
/// 
/// [프리팹 구성]
/// - 루트: 빈 GameObject
/// - 자식: Canvas (Render Mode: World Space)
/// - 자식의 자식: TextMeshProUGUI
/// - 루트에 이 스크립트 부착
/// </summary>
public class DamagePopup : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private TextMeshProUGUI _text;
    [SerializeField] private CanvasGroup _canvasGroup;

    [Header("Animation")]
    [SerializeField] private float _lifetime = 1.2f;
    [SerializeField] private float _floatSpeed = 1.5f;
    [SerializeField] private float _sideSpread = 0.5f;
    [SerializeField] private AnimationCurve _scaleCurve = AnimationCurve.EaseInOut(0, 0.5f, 1, 1f);

    [Header("Colors")]
    [SerializeField] private Color _normalColor = new Color(1f, 0.9f, 0.2f);  // 노란색
    [SerializeField] private Color _criticalColor = new Color(1f, 0.3f, 0.2f); // 빨간색
    [SerializeField] private Color _playerHitColor = new Color(1f, 0.4f, 0.4f); // 피격 색상

    // ── 내부 ──
    private float _elapsed = 0f;
    private Vector3 _velocity;
    private Camera _mainCamera;
    private Transform _originalParent;

    public void Initialize(float damage, Vector3 worldPos, DamageType type = DamageType.Normal)
    {
        transform.position = worldPos;

        // 텍스트 설정
        _text.text = Mathf.RoundToInt(damage).ToString();

        // 색상
        _text.color = type switch
        {
            DamageType.Critical => _criticalColor,
            DamageType.PlayerHit => _playerHitColor,
            _ => _normalColor
        };

        // 크리티컬은 더 크게
        transform.localScale = type == DamageType.Critical
            ? Vector3.one * 1.3f
            : Vector3.one;

        // 이동 방향 (위 + 약간 옆으로)
        float sideX = Random.Range(-_sideSpread, _sideSpread);
        _velocity = new Vector3(sideX, _floatSpeed, 0f);

        _elapsed = 0f;

        if (_canvasGroup != null)
            _canvasGroup.alpha = 1f;
    }

    private void Awake()
    {
        _mainCamera = Camera.main;
        if (_text == null) _text = GetComponentInChildren<TextMeshProUGUI>();
        if (_canvasGroup == null) _canvasGroup = GetComponentInChildren<CanvasGroup>();
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;
        float t = _elapsed / _lifetime;

        // 위로 이동 (중력처럼 감속)
        transform.position += _velocity * Time.deltaTime;
        _velocity.y = Mathf.Max(_velocity.y - 3f * Time.deltaTime, 0f);

        // 카메라 바라보기 (빌보드)
        if (_mainCamera != null)
            transform.rotation = _mainCamera.transform.rotation;

        // 스케일 애니메이션
        float scaleFactor = _scaleCurve.Evaluate(t);
        transform.localScale = Vector3.one * scaleFactor;

        // 페이드 아웃 (후반부)
        if (_canvasGroup != null && t > 0.5f)
        {
            _canvasGroup.alpha = Mathf.Lerp(1f, 0f, (t - 0.5f) / 0.5f);
        }

        // 수명 만료 시 제거
        if (t >= 1f)
            Destroy(gameObject);
    }

    public enum DamageType
    {
        Normal,
        Critical,
        PlayerHit
    }
}
