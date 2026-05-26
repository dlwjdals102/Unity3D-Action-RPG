using UnityEngine;
using System;

/// <summary>
/// 플레이어의 스태미나 관리.
/// 소울라이크 표준 자원 시스템: 공격, 회피, 달리기 모두 소모.
/// 행동 후 일정 지연 후 자동 회복.
/// 단일 책임: 스태미나 추적 + 소모 + 회복.
/// 
/// 정적 설정(최대치/회복률/지연)은 PlayerStatsConfig(SO)에서 읽고,
/// 현재 스태미나(런타임 상태)는 이 컴포넌트가 관리.
/// 값 변경은 SetStamina 한 곳을 경유해 OnStaminaChanged 를 발행(단일 출처).
/// </summary>
public class PlayerStamina : MonoBehaviour
{
    [Header("Stats")]
    [Tooltip("플레이어 정적 스탯 (최대 스태미나/회복 설정). PlayerStatsConfig 에셋 할당")]
    [SerializeField] private PlayerStatsConfig _stats;

    private float _currentStamina;
    private float _regenTimer;

    /// <summary>스태미나 변경 시 발행. (현재, 최대). HUD 등이 구독.</summary>
    public event Action<float, float> OnStaminaChanged;

    // === Public Properties ===
    public float CurrentStamina => _currentStamina;
    public float MaxStamina => _stats != null ? _stats.MaxStamina : 100f;
    private float RegenRate => _stats != null ? _stats.StaminaRegenRate : 30f;
    private float RegenDelay => _stats != null ? _stats.StaminaRegenDelay : 0.5f;

    /// <summary>0.0 ~ 1.0 범위. UI (스태미나 바) 등 활용.</summary>
    public float NormalizedStamina => _currentStamina / MaxStamina;

    private void Awake()
    {
        if (_stats == null)
        {
            Debug.LogError($"[PlayerStamina] PlayerStatsConfig not assigned on {gameObject.name}!");
        }

        _currentStamina = MaxStamina;
    }

    private void Start()
    {
        // 초기값을 HUD 등에 알림 (구독자가 Awake 이후 등록될 수 있어 Start 에서 발행)
        OnStaminaChanged?.Invoke(_currentStamina, MaxStamina);
    }

    private void Update()
    {
        // 회복 지연 타이머
        if (_regenTimer > 0f)
        {
            _regenTimer -= Time.deltaTime;
            return;
        }

        // 자동 회복 (지연 끝난 후)
        if (_currentStamina < MaxStamina)
        {
            SetStamina(_currentStamina + RegenRate * Time.deltaTime);
        }
    }

    // ========================================================================
    // === Public API ===
    // ========================================================================

    /// <summary>
    /// 스태미나가 amount 이상 있는지 확인. 행동 가능 여부 체크 시 사용.
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

        SetStamina(_currentStamina - amount);
        _regenTimer = RegenDelay;
        return true;
    }

    /// <summary>
    /// 지속적 소모. 달리기 등. MoveState 가 매 프레임 호출.
    /// 부족해도 0 까지만 소모 (음수 방지).
    /// </summary>
    public void ConsumeContinuous(float amountPerSecond)
    {
        SetStamina(_currentStamina - amountPerSecond * Time.deltaTime);
        _regenTimer = RegenDelay;
    }

    // ========================================================================
    // === Internal ===
    // ========================================================================

    /// <summary>
    /// 스태미나 값 설정 단일 출처. Clamp 후 실제 변화가 있을 때만 이벤트 발행.
    /// (회복 완료 후 매 프레임 불필요한 발행 방지)
    /// </summary>
    private void SetStamina(float value)
    {
        value = Mathf.Clamp(value, 0f, MaxStamina);
        if (Mathf.Approximately(value, _currentStamina)) return;

        _currentStamina = value;
        OnStaminaChanged?.Invoke(_currentStamina, MaxStamina);
    }
}