using UnityEngine;

/// <summary>
/// 적의 수치 데이터를 담는 ScriptableObject.
/// 잡몹 종류별로 Asset 을 만들어 "같은 코드 + 다른 데이터" 구조를 구현한다.
/// 예: Zombie_Config (약함), Knight_Config (중간), Elite_Config (강함).
/// 
/// Config 에 담는 것: 수치 데이터 (체력, 속도, 거리, 데미지 등).
/// Config 에 담지 않는 것: 인스턴스 참조 (HitOrigin Transform, Target),
/// 시각 보정값 (_damageTextHeight), LayerMask (_targetLayer) - 컴포넌트가 보유.
/// </summary>
[CreateAssetMenu(fileName = "EnemyConfig", menuName = "Hollow Blade/Enemy Config")]
public class EnemyConfig : ScriptableObject
{
    [Header("Health")]
    [SerializeField] private int _maxHealth = 50;

    [Header("Movement")]
    [SerializeField] private float _walkSpeed = 2f;
    [SerializeField] private float _chaseSpeed = 4f;

    [Header("Combat")]
    [Tooltip("공격 거리. ChaseState 에서 이 거리 이내면 AttackState 전환")]
    [SerializeField] private float _attackRange = 1.5f;

    [Tooltip("공격 시 적용되는 데미지")]
    [SerializeField] private int _damage = 10;

    [Header("Vision")]
    [Tooltip("시야 감지 거리")]
    [SerializeField] private float _detectionRange = 8f;

    [Tooltip("시야각 (총 각도, 정면 기준 좌우 절반씩)")]
    [SerializeField] private float _detectionAngle = 90f;

    [Tooltip("추격 포기 거리")]
    [SerializeField] private float _giveUpRange = 15f;

    [Header("Attack Cooldown")]
    [Tooltip("공격 후 다음 공격까지 대기 시간(초). 모든 적 공통. " +
             "0 = 쿨다운 없이 연속 공격. 근접 단타/원거리 발사/엘리트 콤보 모두 이 값을 사용")]
    [SerializeField] private float _attackCooldown = 0f;

    // === Public Properties (읽기 전용) ===
    public int MaxHealth => _maxHealth;
    public float WalkSpeed => _walkSpeed;
    public float ChaseSpeed => _chaseSpeed;
    public float AttackRange => _attackRange;
    public int Damage => _damage;
    public float DetectionRange => _detectionRange;
    public float DetectionAngle => _detectionAngle;
    public float GiveUpRange => _giveUpRange;
    public float AttackCooldown => _attackCooldown;
}