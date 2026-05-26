using UnityEngine;

/// <summary>
/// 플레이어의 정적 스탯 데이터 (ScriptableObject).
/// 최대 체력/스태미나, 스태미나 회복 설정 등 "변하지 않는 초기값/설정" 을 담는다.
/// 
/// 주의: 현재 체력/현재 스태미나 같은 "런타임 상태" 는 여기 넣지 않는다.
/// SO 는 에셋이라 런타임 변경이 영구 저장되어, 다음 플레이에 이전 상태가 남는 버그가 생긴다.
/// 런타임 상태는 PlayerHealth/PlayerStamina 컴포넌트가 관리하고, 이 SO 에선 초기값만 읽는다.
/// (EnemyConfig 와 동일한 패턴: 정적 데이터는 SO, 런타임은 컴포넌트)
/// 
/// 확장: 장비/세이브 단계에서 공격력, 무게, 추가 스탯 등을 여기에 더할 수 있다.
/// </summary>
[CreateAssetMenu(fileName = "PlayerStatsConfig", menuName = "Hollow Blade/Player Stats Config")]
public class PlayerStatsConfig : ScriptableObject
{
    [Header("Health")]
    [Tooltip("최대 체력")]
    [SerializeField] private int _maxHealth = 100;

    [Header("Stamina")]
    [Tooltip("최대 스태미나")]
    [SerializeField] private float _maxStamina = 100f;

    [Tooltip("스태미나 초당 회복량")]
    [SerializeField] private float _staminaRegenRate = 30f;

    [Tooltip("스태미나 소모 후 회복 시작까지 지연(초)")]
    [SerializeField] private float _staminaRegenDelay = 0.5f;

    // === Public Properties (읽기 전용) ===
    public int MaxHealth => _maxHealth;
    public float MaxStamina => _maxStamina;
    public float StaminaRegenRate => _staminaRegenRate;
    public float StaminaRegenDelay => _staminaRegenDelay;
}