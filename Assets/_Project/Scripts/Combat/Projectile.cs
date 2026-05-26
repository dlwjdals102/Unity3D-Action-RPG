using UnityEngine;

/// <summary>
/// 발사체. 발사 방향으로 직선 이동하며, 대상(Player) 또는 장애물과 충돌 시 처리한다.
/// 시각 (화살/마법 구체) 은 Prefab 으로, 수치 (속도/데미지) 는 Initialize 로 주입.
/// → 같은 코드 + 다른 Prefab/데이터 = 궁수의 화살 / 마법사의 구체.
/// 
/// 충돌 감지: 본인 공격 시스템 (PlayerAttacker/MeleeEnemyAttacker) 의 OverlapSphere 패턴 일관.
/// - targetLayer (Player) 충돌: 데미지 적용 + 소멸
/// - obstacleLayer (벽) 충돌: 데미지 없이 소멸
/// 
/// 미래 확장: 풀링 (대량 탄막 시), 유도 발사체, 관통 등.
/// </summary>
public class Projectile : MonoBehaviour
{
    [Header("Hit Detection")]
    [Tooltip("충돌 감지 반경 (m)")]
    [SerializeField] private float _hitRadius = 0.3f;

    [Tooltip("장애물 Layer (벽 등). 맞으면 데미지 없이 소멸")]
    [SerializeField] private LayerMask _obstacleLayer;

    [Header("Lifetime")]
    [Tooltip("자동 소멸 시간 (초). 아무것도 안 맞고 날아가면 이 시간 후 소멸")]
    [SerializeField] private float _lifetime = 5f;

    // === Initialize 로 주입되는 데이터 ===
    private float _speed;
    private int _damage;
    private LayerMask _targetLayer;
    private GameObject _owner;

    private bool _initialized;

    /// <summary>
    /// 발사체 데이터 주입. RangedEnemyAttacker 가 생성 직후 호출.
    /// </summary>
    public void Initialize(float speed, int damage, LayerMask targetLayer, GameObject owner)
    {
        _speed = speed;
        _damage = damage;
        _targetLayer = targetLayer;
        _owner = owner;
        _initialized = true;
    }

    private void Start()
    {
        // 자동 소멸 예약 (아무것도 안 맞아도 lifetime 후 소멸)
        Destroy(gameObject, _lifetime);
    }

    private void Update()
    {
        if (!_initialized) return;

        // 1. 직선 이동 (발사 방향 = transform.forward)
        transform.position += transform.forward * _speed * Time.deltaTime;

        // 2. 충돌 감지
        CheckCollision();
    }

    /// <summary>
    /// OverlapSphere 로 현재 위치에서 충돌 감지.
    /// targetLayer (데미지 + 소멸) 와 obstacleLayer (소멸) 둘 다 체크.
    /// </summary>
    private void CheckCollision()
    {
        // targetLayer + obstacleLayer 둘 다 한 번에 감지
        Collider[] hits = Physics.OverlapSphere(transform.position, _hitRadius, _targetLayer | _obstacleLayer);

        foreach (var hit in hits)
        {
            // 발사한 본인(owner) 은 무시 (자기 발사체에 자기가 안 맞음)
            if (_owner != null && hit.gameObject == _owner) continue;

            int hitLayerBit = 1 << hit.gameObject.layer;

            // 대상(Player) 충돌: 데미지 적용 + 소멸
            if ((_targetLayer.value & hitLayerBit) != 0)
            {
                if (hit.TryGetComponent<IDamageable>(out var target))
                {
                    var info = new DamageInfo
                    {
                        Amount = _damage,
                        Source = _owner,
                        HitPoint = transform.position
                    };
                    target.TakeDamage(info);
                }

                Destroy(gameObject);
                return;
            }

            // 장애물 충돌: 데미지 없이 소멸
            if ((_obstacleLayer.value & hitLayerBit) != 0)
            {
                Destroy(gameObject);
                return;
            }
        }
    }

    // ========================================================================
    // === Editor Visualization ===
    // ========================================================================

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.magenta;
        Gizmos.DrawWireSphere(transform.position, _hitRadius);
    }
}