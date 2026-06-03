using UnityEngine;

/// <summary>
/// 보스의 수치 데이터. EnemyConfig 를 상속.
/// 공통 필드(체력, 속도, AttackRange, Damage, 시야)는 베이스에서 상속하되,
/// 보스는 높은 체력과 별도 밸런스를 가진다.
/// 
/// [1] 기본 골격 단계: 기본 근접 공격만.
/// 추후 단계에서 추가 예정:
/// - [2] 패턴별 데미지/거리 (콤보, 돌진, 범위 공격)
/// - [3] 페이즈 전환 HP 임계값
/// </summary>
[CreateAssetMenu(fileName = "BossConfig", menuName = "Hollow Blade/Boss Config")]
public class BossConfig : EnemyConfig
{
    [Header("Phase (페이즈) - [3]단계에서 사용 예정")]
    [Tooltip("페이즈 2 전환 HP 비율 (0~1). 예: 0.5 = HP 50% 이하 시 페이즈 2")]
    [SerializeField] private float _phase2HealthThreshold = 0.5f;

    [Header("Charge (돌진) - 페이즈 2")]
    [Tooltip("돌진 전 예비 동작 시간(초). 길수록 묵직 + 회피 타이밍 명확")]
    [SerializeField] private float _chargeWindupTime = 1f;

    [Tooltip("돌진 이동 속도(m/s)")]
    [SerializeField] private float _chargeSpeed = 14f;

    [Tooltip("돌진 최대 이동 거리. 빗나가도 이 거리 후 멈춤")]
    [SerializeField] private float _chargeMaxDistance = 12f;

    [Tooltip("돌진 충돌 데미지 (강한 한 방)")]
    [SerializeField] private int _chargeDamage = 35;

    [Tooltip("돌진 후 경직 시간(초). 큰 공격 후 빈틈 (반격 기회)")]
    [SerializeField] private float _chargeStunDuration = 2f;

    [Header("Slam (내려찍기) - 페이즈 2, 근거리")]
    [Tooltip("내려찍기 전 예비 동작 시간(초). 길수록 묵직 + 회피 타이밍 명확")]
    [SerializeField] private float _slamWindupTime = 1.2f;

    [Tooltip("내려찍기 타격 반경(m). 제자리 원형 범위 (돌진의 직선과 다름)")]
    [SerializeField] private float _slamRadius = 3.5f;

    [Tooltip("내려찍기 데미지 (강한 한 방)")]
    [SerializeField] private int _slamDamage = 40;

    [Tooltip("내려찍기 후 후딜(초). 큰 공격 후 빈틈 (반격 기회)")]
    [SerializeField] private float _slamRecoveryTime = 1.8f;

    [Tooltip("내려찍기 발동 최대 거리. 이보다 멀면 후보에서 제외 (근거리 전용 - 돌진과 분리)")]
    [SerializeField] private float _slamMaxRange = 3f;

    [Tooltip("추격 복귀 거리. 보스가 이보다 멀어지면 패턴 대신 추격해 다시 붙는다 (근접 범위 + 여유)")]
    [SerializeField] private float _chaseResumeDistance = 4f;

    // === Public Properties ===
    public float Phase2HealthThreshold => _phase2HealthThreshold;

    public float ChargeWindupTime => _chargeWindupTime;
    public float ChargeSpeed => _chargeSpeed;
    public float ChargeMaxDistance => _chargeMaxDistance;
    public int ChargeDamage => _chargeDamage;
    public float ChargeStunDuration => _chargeStunDuration;

    public float SlamWindupTime => _slamWindupTime;
    public float SlamRadius => _slamRadius;
    public int SlamDamage => _slamDamage;
    public float SlamRecoveryTime => _slamRecoveryTime;
    public float SlamMaxRange => _slamMaxRange;

    public float ChaseResumeDistance => _chaseResumeDistance;
}