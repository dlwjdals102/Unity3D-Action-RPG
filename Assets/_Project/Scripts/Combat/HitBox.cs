using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.TextCore.Text;

/// <summary>
/// 공격 판정 Collider. 무기나 주먹 등에 부착합니다.
/// 데미지는 소유자의 스탯에서 가져옵니다 (HitBox 자체에 데미지 없음).
/// 
/// [동작 원리]
/// 1. 평소에는 비활성 (Collider disabled)
/// 2. Animation Event "OnHitFrame"에서 활성화
/// 3. 소유자의 PlayerStats.TotalAttack으로 데미지 계산
/// 4. 한 공격당 같은 대상에게 1회만 히트 (중복 방지)
/// 5. 공격 종료 시 비활성화 + 히트 목록 초기화
/// </summary>
[RequireComponent(typeof(Collider))]
public class HitBox : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  설정
    // ════════════════════════════════════════════════════

    [Header("Combat Settings")]
    [Tooltip("데미지 배율 (스탯 데미지 * 이 값). 콤보 단계별로 코드에서 변경 가능")]
    [SerializeField] private float _damageMultiplier = 1f;
    [SerializeField] private Define.DamageType _damageType = Define.DamageType.Physical;
    [SerializeField] private float _knockbackForce = 5f;
    [SerializeField] private bool _applyHitStop = true;

    [Header("Hitbox Size (맨손)")]
    [SerializeField] private Vector3 _unarmedSize = new Vector3(0.4f, 0.4f, 0.4f);
    [SerializeField] private Vector3 _unarmedCenter = new Vector3(0f, 0f, 0.3f);

    [Header("References")]
    [SerializeField] private Transform _owner;

    // ════════════════════════════════════════════════════
    //  내부 상태
    // ════════════════════════════════════════════════════

    private Collider _collider;
    private HashSet<int> _hitTargets = new HashSet<int>();
    private bool _isActive = false;
    private bool _suppressed = false;
    private PlayerStats _ownerStats;

    // 맨손 기본 크기 저장
    private Vector3 _defaultSize;
    private Vector3 _defaultCenter;

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

        _ownerStats = _owner.GetComponent<PlayerStats>();

        // 기본 크기 저장
        SaveDefaultSize();

        // 맨손 크기로 초기화
        ApplyUnarmedSize();
    }

    // ════════════════════════════════════════════════════
    //  활성화 / 비활성화
    // ════════════════════════════════════════════════════

    public void EnableHitBox()
    {
        if (_suppressed) return;

        _hitTargets.Clear();
        _isActive = true;
        _collider.enabled = true;
    }

    public void DisableHitBox()
    {
        _isActive = false;
        _collider.enabled = false;
        _hitTargets.Clear();
    }

    /// <summary>히트박스를 억제합니다 (스킬 중 무기 히트박스 비활성).</summary>
    public void SetSuppressed(bool suppressed)
    {
        _suppressed = suppressed;
        if (suppressed)
            DisableHitBox();
    }

    /// <summary>데미지 배율을 설정합니다 (콤보 단계별 차등).</summary>
    public void SetDamageMultiplier(float multiplier)
    {
        _damageMultiplier = multiplier;
    }

    // ════════════════════════════════════════════════════
    //  히트박스 크기 변경 (무기별)
    // ════════════════════════════════════════════════════

    /// <summary>무기에 맞는 히트박스 크기를 적용합니다.</summary>
    public void SetWeaponSize(Vector3 size, Vector3 center)
    {
        if (_collider is BoxCollider box)
        {
            box.size = size;
            box.center = center;
        }
    }

    /// <summary>맨손 크기로 복원합니다.</summary>
    public void ApplyUnarmedSize()
    {
        if (_collider is BoxCollider box)
        {
            box.size = _unarmedSize;
            box.center = _unarmedCenter;
        }
    }

    private void SaveDefaultSize()
    {
        if (_collider is BoxCollider box)
        {
            _defaultSize = box.size;
            _defaultCenter = box.center;
        }
    }

    // ════════════════════════════════════════════════════
    //  충돌 감지
    // ════════════════════════════════════════════════════

    private void OnTriggerEnter(Collider other)
    {
        if (!_isActive) return;

        int targetId = other.gameObject.GetInstanceID();
        if (_hitTargets.Contains(targetId)) return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null) return;
        if (!damageable.IsAlive) return;

        if (other.transform.root == _owner.root) return;

        _hitTargets.Add(targetId);

        Vector3 hitPoint = other.ClosestPoint(transform.position);
        Vector3 knockbackDir = (other.transform.position - _owner.position).normalized;
        knockbackDir.y = 0f;

        // 데미지 = 스탯에서만 가져옴
        float finalDamage;
        if (_ownerStats != null)
        {
            finalDamage = _ownerStats.CalculateOutgoingDamage() * _damageMultiplier;
        }
        else
        {
            // 적 HitBox 등 스탯이 없는 경우 — Inspector 배율로 대체
            finalDamage = 10f * _damageMultiplier;
        }

        DamageData data = new DamageData(
            amount: finalDamage,
            type: _damageType,
            attacker: _owner.gameObject,
            hitPoint: hitPoint,
            knockbackDir: knockbackDir,
            knockbackForce: _knockbackForce,
            applyHitStop: _applyHitStop
        );

        damageable.TakeDamage(data);

        if (_applyHitStop && GameManager.HasInstance)
            GameManager.Instance.HitStop(0.08f, 0.05f);

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