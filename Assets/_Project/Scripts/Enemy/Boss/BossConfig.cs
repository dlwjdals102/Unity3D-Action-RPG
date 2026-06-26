using UnityEngine;

/// <summary>
/// 거울 듀얼리스트 보스의 수치 데이터. EnemyConfig 상속.
/// 공통 필드(체력/속도/AttackRange/Damage/시야/GiveUpRange)는 베이스에서.
/// 원칙: 현재 구현된 행동의 필드만 둔다 (행동 스포크 추가 시 그 필드도 함께).
/// </summary>
[CreateAssetMenu(fileName = "BossConfig", menuName = "Hollow Blade/Boss Config")]
public class BossConfig : EnemyConfig
{
    [Header("Phase (페이즈)")]
    [Tooltip("페이즈 2 전환 HP 비율 (0~1). 예: 0.5 = HP 50% 이하 시 페이즈 2")]
    [SerializeField] private float _phase2HealthThreshold = 0.5f;

    [Header("Bow (활) - 원거리 견제")]
    [Tooltip("발사 투사체 프리팹 (원거리 적과 동일 Projectile)")]
    [SerializeField] private GameObject _bowProjectilePrefab;
    [Tooltip("투사체 속도")]
    [SerializeField] private float _bowProjectileSpeed = 14f;
    [Tooltip("활 데미지")]
    [SerializeField] private int _bowDamage = 20;
    [Tooltip("발사 후 회복(초)")]
    [SerializeField] private float _bowRecoveryTime = 0.6f;
    [Tooltip("활 재사용 쿨다운(초)")]
    [SerializeField] private float _bowCooldown = 4f;
    [Tooltip("활 발동 최소 거리 (이보다 가까우면 활 안 씀)")]
    [SerializeField] private float _bowMinDistance = 5f;
    [Tooltip("활 발동 최대 거리 (이보다 멀면 계속 접근)")]
    [SerializeField] private float _bowMaxDistance = 16f;

    [Header("Melee (근접 패턴)")]
    [Tooltip("발차기 데미지 (빠르고 약한 견제)")]
    [SerializeField] private int _kickDamage = 12;
    [Tooltip("발차기 후 회복(초). 짧게")]
    [SerializeField] private float _kickRecoveryTime = 0.4f;

    [Tooltip("베기 데미지 (느리고 강한 한 방)")]
    [SerializeField] private int _slashDamage = 25;
    [Tooltip("베기 후 회복(초). 길게 → 반격 창")]
    [SerializeField] private float _slashRecoveryTime = 0.8f;

    [Header("Phase 2 (페이즈2 공격성↑)")]
    [Tooltip("페이즈2 쿨다운 배수 (1 미만 = 단축). 예: 0.6 = 40% 빨라짐. 활·근접 재사용 간격에 적용")]
    [SerializeField] private float _phase2CooldownMultiplier = 0.6f;
    [Tooltip("페이즈2 접근 속도 배수 (1 초과 = 빠름). 예: 1.25. 애니 속도와 맞춰 풋슬라이드 방지")]
    [SerializeField] private float _phase2SpeedMultiplier = 1.25f;
    [Tooltip("페이즈2 애니메이션 속도 배수 (1 초과 = 빠름). 예: 1.2. 공격 모션이 빨라져 스윙이 날카로워짐")]
    [SerializeField] private float _phase2AnimSpeedMultiplier = 1.2f;

    [Header("Guard (반사 가드)")]
    [Tooltip("가드 재사용 쿨다운(초). 페이즈2에서 단축됨")]
    [SerializeField] private float _guardCooldown = 6f;
    [Tooltip("근접 행동 시 가드를 택할 확률(0~1). 나머지는 발차기/베기. 예측 불가성")]
    [SerializeField] private float _guardChance = 0.35f;

    // === Public Properties ===
    public float Phase2HealthThreshold => _phase2HealthThreshold;

    public GameObject BowProjectilePrefab => _bowProjectilePrefab;
    public float BowProjectileSpeed => _bowProjectileSpeed;
    public int BowDamage => _bowDamage;
    public float BowRecoveryTime => _bowRecoveryTime;
    public float BowCooldown => _bowCooldown;
    public float BowMinDistance => _bowMinDistance;
    public float BowMaxDistance => _bowMaxDistance;
    public int KickDamage => _kickDamage;
    public float KickRecoveryTime => _kickRecoveryTime;
    public int SlashDamage => _slashDamage;
    public float SlashRecoveryTime => _slashRecoveryTime;
    public float Phase2CooldownMultiplier => _phase2CooldownMultiplier;
    public float Phase2SpeedMultiplier => _phase2SpeedMultiplier;
    public float Phase2AnimSpeedMultiplier => _phase2AnimSpeedMultiplier;
    public float GuardCooldown => _guardCooldown;
    public float GuardChance => _guardChance;
}