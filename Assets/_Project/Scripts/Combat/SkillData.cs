using UnityEngine;

/// <summary>
/// ScriptableObject 기반 스킬 데이터 정의.
/// 각 스킬을 에셋으로 만들어 Inspector에서 수치를 조정할 수 있습니다.
/// 
/// [면접 포인트]
/// - Data-Driven Design: 기획자가 코드 수정 없이 새 스킬을 만들 수 있음
/// - ScriptableObject: 메모리 효율적 (여러 인스턴스가 같은 데이터 공유)
/// - 확장성: 새 스킬 추가 시 SO 에셋만 생성하면 됨
/// 
/// [사용법]
/// Project 창 → 우클릭 → Create → DarkBlade → Skill Data
/// </summary>
[CreateAssetMenu(fileName = "NewSkill", menuName = "DarkBlade/Skill Data")]
public class SkillData : ScriptableObject
{
    // ════════════════════════════════════════════════════
    //  기본 정보
    // ════════════════════════════════════════════════════

    [Header("Basic Info")]
    [Tooltip("스킬 표시 이름")]
    public string skillName = "New Skill";

    [Tooltip("스킬 설명")]
    [TextArea(2, 4)]
    public string description = "";

    [Tooltip("스킬 아이콘 (UI용)")]
    public Sprite icon;

    [Tooltip("스킬 인덱스 (Animator의 SkillIndex 파라미터와 매칭)")]
    public int skillIndex = 0;

    // ════════════════════════════════════════════════════
    //  데미지
    // ════════════════════════════════════════════════════

    [Header("Damage")]
    [Tooltip("기본 데미지")]
    public float baseDamage = 30f;

    [Tooltip("데미지 타입")]
    public Define.DamageType damageType = Define.DamageType.Magical;

    [Tooltip("넉백 세기")]
    public float knockbackForce = 8f;

    // ════════════════════════════════════════════════════
    //  쿨다운
    // ════════════════════════════════════════════════════

    [Header("Cooldown")]
    [Tooltip("쿨다운 시간 (초)")]
    public float cooldown = 5f;

    // ════════════════════════════════════════════════════
    //  범위
    // ════════════════════════════════════════════════════

    [Header("Range")]
    [Tooltip("스킬 범위 타입")]
    public SkillRangeType rangeType = SkillRangeType.Forward;

    [Tooltip("스킬 범위 (반경 또는 거리)")]
    public float range = 3f;

    [Tooltip("범위 각도 (원뿔형일 때)")]
    [Range(0f, 360f)]
    public float angle = 90f;

    // ════════════════════════════════════════════════════
    //  이펙트 / 연출
    // ════════════════════════════════════════════════════

    [Header("Effects")]
    [Tooltip("스킬 VFX 프리팹 (파티클 등)")]
    public GameObject vfxPrefab;

    [Tooltip("스킬 사운드")]
    public AudioClip sfx;

    [Tooltip("히트스톱 적용")]
    public bool applyHitStop = true;

    [Tooltip("히트스톱 지속시간")]
    public float hitStopDuration = 0.12f;

    [Tooltip("카메라 셰이크 강도 (0이면 없음)")]
    public float cameraShakeIntensity = 0.3f;
}

/// <summary>스킬 범위 타입</summary>
public enum SkillRangeType
{
    Forward,    // 전방 직선
    Circle,     // 자신 중심 원형
    Cone,       // 전방 원뿔형
    Point       // 지정 위치 (타겟팅)
}