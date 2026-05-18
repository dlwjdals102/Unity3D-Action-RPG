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

    /// <summary>
    /// 데미지 텍스트 초기화. DamageTextManager 가 풀에서 가져온 후 호출.
    /// </summary>
    public void Initialize(int damage, Vector3 worldPosition)
    {
        transform.position = worldPosition;
        _startPosition = worldPosition;

        _text.text = damage.ToString();
        _startColor = _text.color;
        _text.color = _startColor;  // 알파 리셋

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
        // Billboard: 카메라 향해 회전 (텍스트가 항상 카메라 정면)
        if (Camera.main != null)
        {
            transform.rotation = Camera.main.transform.rotation;
        }
    }
}