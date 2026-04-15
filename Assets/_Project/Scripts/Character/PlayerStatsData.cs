using UnityEngine;

/// <summary>
/// 플레이어 기초 스탯 데이터 (ScriptableObject).
/// 데이터와 로직을 분리하여, 에디터에서 밸런스를 조정합니다.
/// 
/// [사용법]
/// Project 창 → 우클릭 → Create → DarkBlade → Player Stats Data
/// </summary>
[CreateAssetMenu(fileName = "NewPlayerStats", menuName = "DarkBlade/Player Stats Data")]
public class PlayerStatsData : ScriptableObject
{
    [Header("Base Stats")]
    public float baseAttack = 10f;
    public float baseDefense = 5f;
    public float baseMaxHp = 100f;

    [Header("Growth Per Level")]
    public float attackPerLevel = 3f;
    public float defensePerLevel = 1.5f;
    public float maxHpPerLevel = 15f;

    [Header("Experience")]
    public int expPerKill = 30;
    public int baseExpToLevel = 100;
    public float expScaling = 1.3f;

    [Header("Unarmed Combat")]
    [Tooltip("맨손 공격 기본 데미지 배율 (Attack * 이 값)")]
    public float unarmedDamageMultiplier = 1.0f;
}
