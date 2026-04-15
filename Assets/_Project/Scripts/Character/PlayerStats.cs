using System;
using UnityEngine;

/// <summary>
/// 플레이어 스탯 시스템.
/// ScriptableObject(PlayerStatsData)에서 기초값을 읽고,
/// 런타임에 레벨업/장비를 통해 스탯이 변합니다.
/// 
/// [면접 포인트]
/// - ScriptableObject로 데이터와 로직 분리 (Data-Driven Design)
/// - 이벤트 기반: UI가 스탯 변경을 구독하여 자동 업데이트
/// </summary>
public class PlayerStats : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  설정 — ScriptableObject에서 읽기
    // ════════════════════════════════════════════════════

    [Header("Stats Data")]
    [SerializeField] private PlayerStatsData _data;

    // ════════════════════════════════════════════════════
    //  프로퍼티
    // ════════════════════════════════════════════════════

    public int Level { get; private set; } = 1;
    public int CurrentExp { get; private set; } = 0;
    public int ExpToNextLevel { get; private set; }

    public float Attack => _data.baseAttack + (_data.attackPerLevel * (Level - 1));
    public float Defense => _data.baseDefense + (_data.defensePerLevel * (Level - 1));
    public float MaxHp => _data.baseMaxHp + (_data.maxHpPerLevel * (Level - 1));

    /// <summary>총 공격력 (기본 + 장비)</summary>
    public float TotalAttack
    {
        get
        {
            float total = Attack;
            var equip = GetComponent<EquipmentManager>();
            if (equip != null) total += equip.BonusAttack;
            return total;
        }
    }

    /// <summary>총 방어력 (기본 + 장비)</summary>
    public float TotalDefense
    {
        get
        {
            float total = Defense;
            var equip = GetComponent<EquipmentManager>();
            if (equip != null) total += equip.BonusDefense;
            return total;
        }
    }

    /// <summary>총 최대 HP (기본 + 장비)</summary>
    public float TotalMaxHp
    {
        get
        {
            float total = MaxHp;
            if (_equipment != null) total += _equipment.BonusMaxHp;
            return total;
        }
    }

    /// <summary>경험치 비율 (0~1, UI용)</summary>
    public float ExpRatio => ExpToNextLevel > 0 ? (float)CurrentExp / ExpToNextLevel : 0f;

    /// <summary>맨손 데미지 배율</summary>
    public float UnarmedDamageMultiplier => _data != null ? _data.unarmedDamageMultiplier : 1f;

    // ── 이벤트 ──
    public event System.Action<int, int> OnExpChanged;
    public event System.Action<int> OnLevelUp;
    public event System.Action OnStatsChanged;

    // ── 참조 ──
    private PlayerHealth _health;
    private EquipmentManager _equipment;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Awake()
    {
        _health = GetComponent<PlayerHealth>();
        _equipment = GetComponent<EquipmentManager>();
        CalculateExpToNextLevel();

        // 장비 변경 시 스탯 변경 알림
        if (_equipment != null)
            _equipment.OnEquipmentChanged += (_) => NotifyStatsChanged();

        if (_data == null)
            Debug.LogError("[PlayerStats] PlayerStatsData SO가 연결되지 않았습니다!");
    }

    private void Start()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.OnEnemyKilled += OnEnemyKilled;
    }

    private void OnDestroy()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.OnEnemyKilled -= OnEnemyKilled;
    }

    public void NotifyStatsChanged()
    {
        OnStatsChanged?.Invoke();
    }

    // ════════════════════════════════════════════════════
    //  경험치 / 레벨업
    // ════════════════════════════════════════════════════

    private void OnEnemyKilled(GameObject enemy)
    {
        AddExp(_data.expPerKill);
    }

    public void AddExp(int amount)
    {
        CurrentExp += amount;
        Debug.Log($"[PlayerStats] EXP +{amount} ({CurrentExp}/{ExpToNextLevel})");

        OnExpChanged?.Invoke(CurrentExp, ExpToNextLevel);

        while (CurrentExp >= ExpToNextLevel)
        {
            CurrentExp -= ExpToNextLevel;
            LevelUp();
        }
    }

    private void LevelUp()
    {
        Level++;
        CalculateExpToNextLevel();

        Debug.Log(
            $"[PlayerStats] LEVEL UP! Lv.{Level} | " +
            $"ATK: {Attack:F0} | DEF: {Defense:F0} | HP: {MaxHp:F0}"
        );

        if (_health != null)
            _health.FullHeal();

        OnLevelUp?.Invoke(Level);
        OnStatsChanged?.Invoke();
        OnExpChanged?.Invoke(CurrentExp, ExpToNextLevel);
    }

    private void CalculateExpToNextLevel()
    {
        ExpToNextLevel = Mathf.RoundToInt(_data.baseExpToLevel * Mathf.Pow(_data.expScaling, Level - 1));
    }

    // ════════════════════════════════════════════════════
    //  데미지 계산 헬퍼
    // ════════════════════════════════════════════════════

    /// <summary>공격 시 최종 데미지를 계산합니다.</summary>
    public float CalculateOutgoingDamage()
    {
        return TotalAttack;
    }

    /// <summary>피격 시 경감된 데미지를 계산합니다.</summary>
    public float CalculateIncomingDamage(float rawDamage)
    {
        float defense = TotalDefense;
        float reduction = defense / (defense + 50f);
        return rawDamage * (1f - reduction);
    }
}