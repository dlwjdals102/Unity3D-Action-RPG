using UnityEngine;

/// <summary>
/// 복합 엘리트 적의 수치 데이터. EnemyConfig 를 상속하여 콤보 관련 필드를 추가한다.
/// (Phase 2 에서 돌진 관련 필드 추가 예정)
/// 
/// 공통 필드 (체력, 속도, AttackRange, 시야) 는 베이스 EnemyConfig 에서 상속.
/// - AttackRange: 근접 콤보 공격 거리 (좀비와 유사, Phase 2 에서 돌진 거리 별도 추가).
/// - Damage (공통): 엘리트는 콤보라 사용 안 함. ComboDamages 사용.
/// 
/// 콤보는 확률적 진행: 1타(항상) → [ComboContinueChance[0]] → 2타 → [ComboContinueChance[1]] → 3타.
/// 플레이어의 의도적 3타 콤보와 달리 예측 불가 (거울 콘셉트의 변주).
/// </summary>
[CreateAssetMenu(fileName = "EliteEnemyConfig", menuName = "Hollow Blade/Elite Enemy Config")]
public class EliteEnemyConfig : EnemyConfig
{
    [Header("Combo (콤보)")]
    [Tooltip("각 타의 데미지. 길이 = 최대 콤보 타수 (예: 3타 = {12, 18, 30})")]
    [SerializeField] private int[] _comboDamages = { 12, 18, 30 };

    [Tooltip("다음 타 진행 확률 (0~1). 길이 = ComboDamages 길이 - 1. " +
             "예: {0.7, 0.5} = 1타→2타 70%, 2타→3타 50%")]
    [SerializeField] private float[] _comboContinueChance = { 0.7f, 0.5f };

    [Header("Charge (돌진)")]
    [Tooltip("돌진 이동 속도 (m/s). ChaseSpeed 보다 훨씬 빠름")]
    [SerializeField] private float _chargeSpeed = 12f;

    [Tooltip("돌진 발동 최소 거리. 이보다 가까우면 콤보 (보통 AttackRange 와 같게)")]
    [SerializeField] private float _chargeMinDistance = 2f;

    [Tooltip("돌진 발동 최대 거리. 이보다 멀면 일반 추격(Chase)")]
    [SerializeField] private float _chargeMaxDistance = 8f;

    [Tooltip("돌진 쿨다운(초). 한 번 돌진 후 다음 돌진까지 대기 (반복 방지)")]
    [SerializeField] private float _chargeCooldown = 5f;

    [Tooltip("돌진 충돌 시 데미지 (단발 큰 한 방)")]
    [SerializeField] private int _chargeDamage = 20;

    [Tooltip("돌진 종료/빗나감 후 경직 시간(초). 플레이어 반격 기회 (펀치 윈도우)")]
    [SerializeField] private float _chargeStunDuration = 1.5f;

    [Tooltip("돌진 전 예비동작(Idle 정지) 시간(초)")]
    [SerializeField] private float _chargeWindupDuration = 0.5f;

    // === Public Properties (읽기 전용) ===
    public int[] ComboDamages => _comboDamages;
    public float[] ComboContinueChance => _comboContinueChance;

    // 돌진
    public float ChargeSpeed => _chargeSpeed;
    public float ChargeMinDistance => _chargeMinDistance;
    public float ChargeMaxDistance => _chargeMaxDistance;
    public float ChargeCooldown => _chargeCooldown;
    public int ChargeDamage => _chargeDamage;
    public float ChargeStunDuration => _chargeStunDuration;
    public float ChargeWindupDuration => _chargeWindupDuration;

    /// <summary>최대 콤보 타수 (ComboDamages 길이).</summary>
    public int MaxComboCount => _comboDamages != null ? _comboDamages.Length : 0;

    /// <summary>
    /// 지정 콤보 인덱스의 데미지. 범위 밖이면 0.
    /// </summary>
    public int GetComboDamage(int index)
    {
        if (_comboDamages == null || index < 0 || index >= _comboDamages.Length)
        {
            return 0;
        }
        return _comboDamages[index];
    }

    /// <summary>
    /// 현재 타(index)에서 다음 타로 진행할 확률. 범위 밖이면 0 (진행 안 함).
    /// index 0 = 1타→2타 확률, index 1 = 2타→3타 확률.
    /// </summary>
    public float GetContinueChance(int index)
    {
        if (_comboContinueChance == null || index < 0 || index >= _comboContinueChance.Length)
        {
            return 0f;
        }
        return _comboContinueChance[index];
    }
}