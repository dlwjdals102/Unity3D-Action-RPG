using UnityEngine;

/// <summary>
/// 모든 게임 사운드를 한 곳에 모은 중앙 라이브러리 (ScriptableObject).
/// 각 시스템은 클립/SO 를 직접 들지 않고, AudioManager 를 통해 이 라이브러리의
/// 사운드를 참조해 재생한다 (중앙 집중 + 타입 안전, enum/문자열 ID 불필요).
/// 새 사운드 추가 = 여기 필드 한 줄 + 에셋 할당.
/// </summary>
[CreateAssetMenu(fileName = "SoundLibrary", menuName = "Audio/Sound Library")]
public class SoundLibrary : ScriptableObject
{
    [Header("Combat - Player")]
    public SoundDefinition SwordSwing;
    public SoundDefinition SwordImpact;
    public SoundDefinition Guard;
    public SoundDefinition Parry;
    public SoundDefinition PlayerHurt;

    [Header("Combat - Enemy")]
    public SoundDefinition EnemyAttack;
    public SoundDefinition EnemyHurt;
    public SoundDefinition EnemyDeath;

    [Header("Movement")]
    public SoundDefinition Footstep;

    [Header("UI / System")]
    public SoundDefinition UIClick;
    public SoundDefinition SoulPickup;
}