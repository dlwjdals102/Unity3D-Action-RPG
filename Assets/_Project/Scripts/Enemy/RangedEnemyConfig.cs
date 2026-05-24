using UnityEngine;

/// <summary>
/// 원거리 적의 수치 데이터. EnemyConfig 를 상속하여 발사체 관련 필드를 추가한다.
/// 
/// 공통 필드 (체력, 속도, AttackRange, Damage, 시야) 는 베이스 EnemyConfig 에서 상속.
/// - AttackRange: 원거리에선 "발사 시작 거리" 의미 (근접 1.5m 보다 큰 값, 예: 6m).
/// - Damage: 발사체가 적중 시 적용하는 데미지.
/// 
/// 발사체 시각 (화살/마법 구체) 은 ProjectilePrefab 으로 데이터화.
/// 같은 원거리 코드 + 다른 Config → 궁수(화살) / 마법사(마법 구체).
/// </summary>
[CreateAssetMenu(fileName = "RangedEnemyConfig", menuName = "Hollow Blade/Ranged Enemy Config")]
public class RangedEnemyConfig : EnemyConfig
{
    [Header("Ranged (발사체)")]
    [Tooltip("발사체 Prefab (화살, 마법 구체 등). Projectile 컴포넌트 필요")]
    [SerializeField] private GameObject _projectilePrefab;

    [Tooltip("발사체 이동 속도 (m/s)")]
    [SerializeField] private float _projectileSpeed = 10f;

    [Tooltip("발사 주기 (초). 한 발 발사 후 다음 발사까지 대기 시간")]
    [SerializeField] private float _attackCooldown = 2f;

    // === Public Properties (읽기 전용) ===
    public GameObject ProjectilePrefab => _projectilePrefab;
    public float ProjectileSpeed => _projectileSpeed;
    public float AttackCooldown => _attackCooldown;
}