using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;   // Vignette

/// <summary>
/// 플레이어 피격/위험 시 화면 가장자리 붉은 비네트 (카메라 멀미 없이 피드백).
/// - 펄스: 맞을 때마다 데미지 비례로 확 켜졌다 감쇠 (OnDamaged 구독)
/// - 위험 지속: 체력 낮으면 은은하게 베이스 강도 유지 (OnHealthChanged 구독)
/// 표시 강도 = 낮은체력 베이스 + 펄스. 씬의 Global Volume(Vignette 포함)을 런타임 조절.
/// </summary>
public class PlayerHitVignette : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerHealth _health;
    [Tooltip("Vignette 오버라이드가 있는 Global Volume")]
    [SerializeField] private Volume _volume;

    [Header("색 / 전체 한계")]
    [SerializeField] private Color _vignetteColor = new Color(0.55f, 0f, 0f);
    [Tooltip("비네트 강도 상한 (베이스+펄스 합산 클램프)")]
    [SerializeField] private float _maxIntensity = 0.5f;

    [Header("피격 펄스")]
    [Tooltip("최소 피격 펄스 강도 (약한 타격)")]
    [SerializeField] private float _minPulseIntensity = 0.15f;
    [Tooltip("최대 피격 펄스 강도 (강한 타격)")]
    [SerializeField] private float _maxPulseIntensity = 0.4f;
    [Tooltip("이 데미지에서 펄스 최대")]
    [SerializeField] private int _maxPulseDamage = 40;
    [Tooltip("펄스가 0으로 사라지는 시간(초)")]
    [SerializeField] private float _pulseFadeTime = 0.4f;

    [Header("위험 지속 (낮은 체력)")]
    [Tooltip("이 체력 비율 이하부터 베이스 비네트 시작 (예: 0.3 = 30%)")]
    [SerializeField] private float _lowHealthThreshold = 0.3f;
    [Tooltip("체력 0 일 때 베이스 강도")]
    [SerializeField] private float _maxLowHealthIntensity = 0.3f;
    [Tooltip("임계 체력에서의 베이스 강도(바닥). 넘는 순간 바로 보이게")]
    [SerializeField] private float _minLowHealthIntensity = 0.15f;

    private Vignette _vignette;
    private float _pulse;            // 현재 펄스 강도 (감쇠)
    private float _lowHealthBase;    // 낮은 체력 베이스 강도

    private void Awake()
    {
        if (_health == null) _health = GetComponentInParent<PlayerHealth>();

        if (_volume != null && _volume.profile.TryGet(out _vignette))
        {
            _vignette.color.overrideState = true;
            _vignette.intensity.overrideState = true;
            _vignette.color.value = _vignetteColor;
            _vignette.intensity.value = 0f;
        }
        else
        {
            Debug.LogError($"[PlayerHitVignette] Volume 에 Vignette 오버라이드가 없음 ({gameObject.name}). " +
                           $"Global Volume 프로필에 Vignette 추가 필요.");
        }
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnDamaged += HandleDamaged;
            _health.OnHealthChanged += HandleHealthChanged;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDamaged -= HandleDamaged;
            _health.OnHealthChanged -= HandleHealthChanged;
        }
    }

    /// <summary>피격: 데미지 비례 펄스 (기존보다 약하면 유지 - 큰 펄스 안 깎임).</summary>
    private void HandleDamaged(int damage)
    {
        float t = Mathf.Clamp01((float)damage / _maxPulseDamage);
        float hitPulse = Mathf.Lerp(_minPulseIntensity, _maxPulseIntensity, t);
        _pulse = Mathf.Max(_pulse, hitPulse);
    }

    /// <summary>체력 변경: 임계 이하면 베이스 비네트 램프(낮을수록 진하게).</summary>
    private void HandleHealthChanged(int current, int max)
    {
        float ratio = max > 0 ? (float)current / max : 0f;
        if (ratio <= _lowHealthThreshold)
        {
            float t = Mathf.InverseLerp(_lowHealthThreshold, 0f, ratio);
            _lowHealthBase = Mathf.Lerp(_minLowHealthIntensity, _maxLowHealthIntensity, t);  // ← 바닥부터
        }
        else
        {
            _lowHealthBase = 0f;
        }
    }

    private void Update()
    {
        if (_vignette == null) return;

        // 펄스 감쇠 (히트스톱 timeScale=0 중에도 진행 → unscaled)
        if (_pulse > 0f)
        {
            float rate = _maxPulseIntensity / Mathf.Max(0.01f, _pulseFadeTime);
            _pulse = Mathf.MoveTowards(_pulse, 0f, rate * Time.unscaledDeltaTime);
        }

        float target = Mathf.Clamp(_lowHealthBase + _pulse, 0f, _maxIntensity);
        if (!Mathf.Approximately(_vignette.intensity.value, target))
        {
            _vignette.intensity.value = target;
        }
    }
}