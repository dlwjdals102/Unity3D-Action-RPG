using System.Collections.Generic;
using UnityEngine;
using System;

/// <summary>
/// 스킬 실행 및 쿨다운을 관리합니다.
/// Player에 부착하여 스킬 슬롯별로 SkillData를 할당합니다.
/// 
/// [구조]
/// PlayerInputHandler (Q/E 입력)
///   → PlayerStateMachine (Skill 상태 전환)
///     → SkillExecutor.ExecuteSkill()
///       → 애니메이션 재생 + 범위 판정 + 데미지 적용
/// </summary>
public class SkillExecutor : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  스킬 슬롯
    // ════════════════════════════════════════════════════

    [Header("Skill Slots")]
    [SerializeField] private SkillData _skill1;
    [SerializeField] private SkillData _skill2;

    // ════════════════════════════════════════════════════
    //  쿨다운 추적
    // ════════════════════════════════════════════════════

    private Dictionary<SkillData, float> _cooldownTimers = new Dictionary<SkillData, float>();

    // ── 캐싱 ──
    private PlayerAnimator _animator;
    private Transform _transform;

    // ── 이벤트 ──
    /// <summary>스킬 사용 시 발생 (UI 쿨다운 표시용)</summary>
    public event Action<SkillData> OnSkillUsed;

    /// <summary>쿨다운 완료 시 발생</summary>
    public event Action<SkillData> OnSkillReady;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        _animator = GetComponent<PlayerAnimator>();
        _transform = transform;
    }

    private void Update()
    {
        UpdateCooldowns();
    }

    // ════════════════════════════════════════════════════
    //  쿨다운 관리
    // ════════════════════════════════════════════════════

    private void UpdateCooldowns()
    {
        // 딕셔너리를 순회하면서 쿨다운 감소
        List<SkillData> completedSkills = null;

        foreach (var kvp in _cooldownTimers)
        {
            if (kvp.Value <= 0f) continue;

            // 직접 수정 불가하므로 완료된 것만 추적
            if (kvp.Value - Time.deltaTime <= 0f)
            {
                if (completedSkills == null)
                    completedSkills = new List<SkillData>();
                completedSkills.Add(kvp.Key);
            }
        }

        // 쿨다운 타이머 업데이트 (별도 루프)
        var keys = new List<SkillData>(_cooldownTimers.Keys);
        foreach (var key in keys)
        {
            if (_cooldownTimers[key] > 0f)
                _cooldownTimers[key] -= Time.deltaTime;
        }

        // 완료된 스킬 이벤트 발행
        if (completedSkills != null)
        {
            foreach (var skill in completedSkills)
                OnSkillReady?.Invoke(skill);
        }
    }

    /// <summary>스킬이 사용 가능한지 확인합니다.</summary>
    public bool IsSkillReady(int slotIndex)
    {
        SkillData skill = GetSkillBySlot(slotIndex);
        if (skill == null) return false;

        if (_cooldownTimers.TryGetValue(skill, out float timer))
            return timer <= 0f;

        return true;
    }

    /// <summary>남은 쿨다운 시간을 반환합니다.</summary>
    public float GetRemainingCooldown(int slotIndex)
    {
        SkillData skill = GetSkillBySlot(slotIndex);
        if (skill == null) return 0f;

        if (_cooldownTimers.TryGetValue(skill, out float timer))
            return Mathf.Max(0f, timer);

        return 0f;
    }

    /// <summary>쿨다운 비율을 반환합니다 (0~1, UI 표시용).</summary>
    public float GetCooldownRatio(int slotIndex)
    {
        SkillData skill = GetSkillBySlot(slotIndex);
        if (skill == null) return 0f;

        float remaining = GetRemainingCooldown(slotIndex);
        if (remaining <= 0f) return 0f;

        return remaining / skill.cooldown;
    }

    // ════════════════════════════════════════════════════
    //  스킬 실행
    // ════════════════════════════════════════════════════

    /// <summary>
    /// 스킬을 실행합니다. FSM의 SkillState에서 호출합니다.
    /// </summary>
    /// <param name="slotIndex">스킬 슬롯 (0 = Q, 1 = E)</param>
    /// <returns>실행 성공 여부</returns>
    public bool ExecuteSkill(int slotIndex)
    {
        SkillData skill = GetSkillBySlot(slotIndex);
        if (skill == null)
        {
            Debug.LogWarning($"[SkillExecutor] 슬롯 {slotIndex}에 스킬이 없습니다.");
            return false;
        }

        if (!IsSkillReady(slotIndex))
        {
            Debug.Log($"[SkillExecutor] {skill.skillName} 쿨다운 중 ({GetRemainingCooldown(slotIndex):F1}s)");
            return false;
        }

        // 쿨다운 시작
        _cooldownTimers[skill] = skill.cooldown;

        // 애니메이션 재생
        _animator.PlaySkill(skill.skillIndex);

        // 이벤트 발행
        OnSkillUsed?.Invoke(skill);

        Debug.Log($"[SkillExecutor] {skill.skillName} 발동! 쿨다운: {skill.cooldown}s");

        return true;
    }

    /// <summary>
    /// 스킬의 데미지 판정을 실행합니다.
    /// Animation Event "OnHitFrame" 타이밍에 호출합니다.
    /// </summary>
    public void ApplySkillDamage(int slotIndex)
    {
        SkillData skill = GetSkillBySlot(slotIndex);
        if (skill == null) return;

        // 범위 내 적 탐색
        Collider[] hits = DetectTargets(skill);

        foreach (var hit in hits)
        {
            // 자기 자신 제외
            if (hit.transform.root == _transform.root) continue;

            IDamageable damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable == null || !damageable.IsAlive) continue;

            // 데미지 데이터 생성
            Vector3 hitPoint = hit.ClosestPoint(_transform.position);
            Vector3 knockbackDir = (hit.transform.position - _transform.position).normalized;
            knockbackDir.y = 0f;

            DamageData data = new DamageData(
                amount: skill.baseDamage,
                type: skill.damageType,
                attacker: gameObject,
                hitPoint: hitPoint,
                knockbackDir: knockbackDir,
                knockbackForce: skill.knockbackForce,
                applyHitStop: skill.applyHitStop
            );

            damageable.TakeDamage(data);
        }

        // 히트스톱
        if (skill.applyHitStop && hits.Length > 0 && GameManager.HasInstance)
            GameManager.Instance.HitStop(skill.hitStopDuration, 0.05f);

        // VFX 생성
        if (skill.vfxPrefab != null)
        {
            Vector3 spawnPos = _transform.position + _transform.forward * (skill.range * 0.5f);
            spawnPos.y += 1f;
            GameObject vfx = Instantiate(skill.vfxPrefab, spawnPos, _transform.rotation);
            Destroy(vfx, 3f);
        }
    }

    // ════════════════════════════════════════════════════
    //  범위 판정
    // ════════════════════════════════════════════════════

    private Collider[] DetectTargets(SkillData skill)
    {
        int enemyLayer = Define.Layer.EnemyMask;

        switch (skill.rangeType)
        {
            case SkillRangeType.Circle:
                // 자신 중심 원형 범위
                return Physics.OverlapSphere(
                    _transform.position,
                    skill.range,
                    enemyLayer
                );

            case SkillRangeType.Cone:
                // 전방 원뿔: 일단 구체로 탐색 후 각도 필터링
                Collider[] candidates = Physics.OverlapSphere(
                    _transform.position,
                    skill.range,
                    enemyLayer
                );
                return FilterByCone(candidates, skill.angle);

            case SkillRangeType.Forward:
            default:
                // 전방 직선: 박스 캐스트
                Vector3 center = _transform.position
                    + _transform.forward * (skill.range * 0.5f)
                    + Vector3.up;
                Vector3 halfExtents = new Vector3(1f, 1f, skill.range * 0.5f);
                return Physics.OverlapBox(
                    center,
                    halfExtents,
                    _transform.rotation,
                    enemyLayer
                );
        }
    }

    private Collider[] FilterByCone(Collider[] candidates, float maxAngle)
    {
        List<Collider> filtered = new List<Collider>();
        float halfAngle = maxAngle * 0.5f;

        foreach (var col in candidates)
        {
            Vector3 dirToTarget = (col.transform.position - _transform.position).normalized;
            float angle = Vector3.Angle(_transform.forward, dirToTarget);

            if (angle <= halfAngle)
                filtered.Add(col);
        }

        return filtered.ToArray();
    }

    // ════════════════════════════════════════════════════
    //  유틸리티
    // ════════════════════════════════════════════════════

    private SkillData GetSkillBySlot(int slotIndex)
    {
        return slotIndex switch
        {
            0 => _skill1,
            1 => _skill2,
            _ => null
        };
    }

    /// <summary>슬롯의 SkillData를 반환합니다 (UI용).</summary>
    public SkillData GetSkillData(int slotIndex)
    {
        return GetSkillBySlot(slotIndex);
    }

    // ════════════════════════════════════════════════════
    //  디버그 시각화
    // ════════════════════════════════════════════════════

    private void OnDrawGizmosSelected()
    {
        if (_skill1 != null)
            DrawSkillRange(_skill1, Color.cyan);
        if (_skill2 != null)
            DrawSkillRange(_skill2, Color.magenta);
    }

    private void DrawSkillRange(SkillData skill, Color color)
    {
        Gizmos.color = color;

        switch (skill.rangeType)
        {
            case SkillRangeType.Circle:
                Gizmos.DrawWireSphere(transform.position, skill.range);
                break;

            case SkillRangeType.Forward:
                Vector3 center = transform.position
                    + transform.forward * (skill.range * 0.5f)
                    + Vector3.up;
                Gizmos.matrix = Matrix4x4.TRS(center, transform.rotation, Vector3.one);
                Gizmos.DrawWireCube(Vector3.zero, new Vector3(2f, 2f, skill.range));
                break;

            case SkillRangeType.Cone:
                Gizmos.DrawWireSphere(transform.position, skill.range);
                Vector3 leftDir = Quaternion.Euler(0, -skill.angle * 0.5f, 0) * transform.forward;
                Vector3 rightDir = Quaternion.Euler(0, skill.angle * 0.5f, 0) * transform.forward;
                Gizmos.DrawLine(transform.position, transform.position + leftDir * skill.range);
                Gizmos.DrawLine(transform.position, transform.position + rightDir * skill.range);
                break;
        }
    }
}