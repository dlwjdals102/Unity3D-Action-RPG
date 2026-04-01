using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 공격 판정 Collider. 무기나 주먹 등에 부착합니다.
/// 
/// [동작 원리]
/// 1. 평소에는 비활성 (Collider disabled)
/// 2. Animation Event "OnHitFrame"에서 활성화
/// 3. 트리거 충돌한 HurtBox의 IDamageable에 데미지 전달
/// 4. 한 공격당 같은 대상에게 1회만 히트 (중복 방지)
/// 5. 공격 종료 시 비활성화 + 히트 목록 초기화
/// 
/// [설정]
/// - Collider를 Trigger로 설정
/// - Layer: PlayerAttack 또는 EnemyAttack
/// </summary>
[RequireComponent(typeof(Collider))]
public class HitBox : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  설정
    // ════════════════════════════════════════════════════

    [Header("Damage Settings")]
    [SerializeField] private float _baseDamage = 10f;
    [SerializeField] private Define.DamageType _damageType = Define.DamageType.Physical;
    [SerializeField] private float _knockbackForce = 5f;
    [SerializeField] private bool _applyHitStop = true;

    [Header("References")]
    [SerializeField] private Transform _owner;

    // ════════════════════════════════════════════════════
    //  내부 상태
    // ════════════════════════════════════════════════════

    private Collider _collider;
    private HashSet<int> _hitTargets = new HashSet<int>();
    private bool _isActive = false;

    // ── 이벤트 ──
    /// <summary>대상에 히트했을 때 발생 (이펙트, 사운드 등에서 구독)</summary>
    public event System.Action<DamageData, GameObject> OnHit;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        _collider = GetComponent<Collider>();
        _collider.isTrigger = true;
        _collider.enabled = false;

        if (_owner == null)
            _owner = transform.root;
    }

    // ════════════════════════════════════════════════════
    //  활성화 / 비활성화
    // ════════════════════════════════════════════════════

    /// <summary>
    /// 히트박스를 활성화합니다.
    /// PlayerAnimator의 OnAttackHitFrame 이벤트에서 호출합니다.
    /// </summary>
    public void EnableHitBox()
    {
        _hitTargets.Clear();
        _isActive = true;
        _collider.enabled = true;
    }

    /// <summary>
    /// 히트박스를 비활성화합니다.
    /// 공격 종료 시 또는 일정 시간 후 호출합니다.
    /// </summary>
    public void DisableHitBox()
    {
        _isActive = false;
        _collider.enabled = false;
        _hitTargets.Clear();
    }

    /// <summary>데미지 배율을 설정합니다 (콤보 단계별 다른 데미지).</summary>
    public void SetDamageMultiplier(float multiplier)
    {
        _baseDamage *= multiplier;
    }

    // ════════════════════════════════════════════════════
    //  충돌 감지
    // ════════════════════════════════════════════════════

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive) return;

        // 같은 대상 중복 히트 방지
        int targetId = other.gameObject.GetInstanceID();
        if (_hitTargets.Contains(targetId)) return;

        // IDamageable 인터페이스 검색 (자신 + 부모)
        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;
        if (!damageable.IsAlive) return;

        // 자기 자신 공격 방지
        if (other.transform.root == _owner.root) return;

        // 히트 목록에 추가
        _hitTargets.Add(targetId);

        // 데미지 데이터 생성
        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 knockbackDir = (other.transform.position - _owner.position).normalized;
        knockbackDir.y = 0f;

        DamageData data = new DamageData(
            amount: _baseDamage,
            type: _damageType,
            attacker: _owner.gameObject,
            hitPoint: hitPoint,
            knockbackDir: knockbackDir,
            knockbackForce: _knockbackForce,
            applyHitStop: _applyHitStop
        );

        // 데미지 적용
        float actualDamage = damageable.TakeDamage(data);

        // 히트스톱 연출
        if (_applyHitStop && GameManager.HasInstance)
            GameManager.Instance.HitStop(0.08f, 0.05f);

        // 히트 이벤트 발행 (VFX, SFX에서 사용)
        OnHit?.Invoke(data, other.gameObject);
    }

    // ════════════════════════════════════════════════════
    //  디버그
    // ════════════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        Collider col = GetComponent<Collider>();
        if (col == null) return;

        Gizmos.color = _isActive ? new Color(1f, 0f, 0f, 0.4f) : new Color(1f, 1f, 0f, 0.2f);

        if (col is BoxCollider box)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawCube(box.center, box.size);
        }
        else if (col is SphereCollider sphere)
        {
            Gizmos.DrawSphere(
                transform.position + sphere.center,
                sphere.radius * transform.lossyScale.x
            );
        }
    }
}