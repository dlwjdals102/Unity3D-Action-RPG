using UnityEngine;
using TMPro;

/// <summary>
/// 데미지 텍스트 (Floating Damage Number).
/// 타격 위치에 등장하여 위로 떠오르면서 페이드 아웃, 수명 종료 시 풀로 반환.
/// 단일 책임: 데미지 텍스트의 시각 표현.
/// </summary>
public class DamageText : MonoBehaviour
{
    [Header("Animation")]
    [SerializeField] private float _lifetime = 1f;
    [SerializeField] private float _floatHeight = 1.5f;
    [Tooltip("수명의 몇 % 시점부터 페이드 시작 (0~1)")]
    [SerializeField] private float _fadeStartRatio = 0.5f;

    [Header("Reference")]
    [SerializeField] private TMP_Text _text;

    private float _elapsed;
    private Vector3 _startPosition;
    private Color _startColor;
    private Color _baseColor;
    private bool _baseColorCaptured;

    private void Awake()
    {
        // 프리팹 원본 색을 최초 1회 저장 (페이드로 오염되기 전).
        // 풀 재사용 시 이 원본으로 리셋 → 알파 0(투명) 잔재 방지.
        CaptureBaseColor();
    }

    /// <summary>
    /// 원본 색 캡처 (최초 1회). _text 참조가 있으면 그 색을, 알파는 1 로 보정.
    /// </summary>
    private void CaptureBaseColor()
    {
        if (_baseColorCaptured) return;
        if (_text == null) return;

        _baseColor = _text.color;
        _baseColor.a = 1f;  // 알파는 항상 불투명에서 시작 (혹시 프리팹 알파가 1 아니어도 보정)
        _baseColorCaptured = true;
    }

    /// <summary>
    /// 데미지 텍스트 초기화. DamageTextManager 가 풀에서 가져온 후 호출.
    /// </summary>
    public void Initialize(int damage, Vector3 worldPosition)
    {
        transform.position = worldPosition;
        _startPosition = worldPosition;

        _text.text = damage.ToString();
        // 페이드로 오염될 수 있는 _text.color 대신 원본(_baseColor)에서 시작.
        // (재사용 시 이전 사용의 알파 0 잔재 제거 → 투명 버그 방지)
        _startColor = _baseColor;
        _text.color = _baseColor;

        _elapsed = 0f;

        gameObject.SetActive(true);
    }

    private void Update()
    {
        _elapsed += Time.deltaTime;

        float t = _elapsed / _lifetime;

        // 위로 떠오름
        transform.position = _startPosition + Vector3.up * (_floatHeight * t);

        // 페이드 (수명 후반에 알파 감소)
        if (t > _fadeStartRatio)
        {
            float fadeT = (t - _fadeStartRatio) / (1f - _fadeStartRatio);
            Color color = _startColor;
            color.a = Mathf.Lerp(1f, 0f, fadeT);
            _text.color = color;
        }

        // 수명 종료 → 풀로 반환
        if (_elapsed >= _lifetime)
        {
            DamageTextManager.Instance.ReturnToPool(this);
        }
    }

    private void LateUpdate()
    {
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}