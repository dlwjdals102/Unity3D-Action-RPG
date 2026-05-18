using UnityEngine;

/// <summary>
/// 플레이어의 스태미나 관리.
/// 소울라이크 표준 자원 시스템: 공격, 회피, 달리기 모두 소모.
/// 행동 후 일정 지연 후 자동 회복.
/// 단일 책임: 스태미나 추적 + 소모 + 회복.
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    [Header("Stamina")]
    [SerializeField] private float _maxStamina = 100f;

    [Header("Regeneration")]
    [Tooltip("초당 회복량")]
    [SerializeField] private float _regenRate = 30f;

    [Tooltip("소모 후 회복 시작 대기 시간 (초)")]
    [SerializeField] private float _regenDelay = 0.5f;

    private float _currentStamina;
    private float _regenTimer;

    // === Public Properties ===
    public float CurrentStamina => _currentStamina;
    public float MaxStamina => _maxStamina;

    /// <summary>0.0 ~ 1.0 범위. UI (스태미나 바) 등 활용.</summary>
    public float NormalizedStamina => _currentStamina / _maxStamina;

    private void Awake()
    {
        _currentStamina = _maxStamina;
    }

    private void Update()
    {
        Debug.Log("_currentStamina : " + _currentStamina);

        // 회복 지연 타이머
        if (_regenTimer > 0f)
        {
            _regenTimer -= Time.deltaTime;
            return;
        }

        // 자동 회복 (지연 끝난 후)
        if (_currentStamina < _maxStamina)
        {
            _currentStamina += _regenRate * Time.deltaTime;
            if (_currentStamina > _maxStamina) _currentStamina = _maxStamina;
        }
    }

    // ========================================================================
    // === Public API ===
    // ========================================================================

    /// <summary>
    /// 스태미나가 amount 이상 있는지 확인.
    /// 행동 가능 여부 체크 시 사용.
    /// </summary>
    public bool HasEnough(float amount)
    {
        return _currentStamina >= amount;
    }

    /// <summary>
    /// 이산적 (한 번에) 소모. 공격, 회피 등.
    /// 부족하면 소모 안 함 + false 반환 (소울라이크 표준).
    /// </summary>
    public bool TryConsume(float amount)
    {
        if (!HasEnough(amount)) return false;

        _currentStamina -= amount;
        _regenTimer = _regenDelay;
        return true;
    }

    /// <summary>
    /// 지속적 소모. 달리기 등. MoveState 가 매 프레임 호출.
    /// 부족해도 0 까지만 소모 (음수 방지).
    /// 호출 측에서 HasEnough 로 행동 가능 여부 확인 필요.
    /// </summary>
    public void ConsumeContinuous(float amountPerSecond)
    {
        _currentStamina -= amountPerSecond * Time.deltaTime;
        if (_currentStamina < 0f) _currentStamina = 0f;
        _regenTimer = _regenDelay;
    }
}