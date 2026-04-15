/// <summary>
/// 프로젝트 전역에서 사용되는 Enum, 상수, 태그, 레이어를 정의합니다.
/// 매직 넘버와 하드코딩 문자열을 방지합니다.
/// </summary>
public static class Define
{
    // ════════════════════════════════════════════════════
    //  게임 상태
    // ════════════════════════════════════════════════════
    public enum GameState
    {
        Ready,
        Playing,
        Paused,
        GameOver,
        Loading
    }

    // ════════════════════════════════════════════════════
    //  캐릭터 상태 (FSM에서 사용)
    // ════════════════════════════════════════════════════
    public enum CharacterState
    {
        Idle,
        Move,
        Attack,
        Skill,
        Dodge,
        Hit,
        Die
    }

    // ════════════════════════════════════════════════════
    //  공격 관련
    // ════════════════════════════════════════════════════
    public enum AttackType
    {
        Light,          // 약공격
        Heavy,          // 강공격
        Skill           // 스킬 공격
    }

    public enum DamageType
    {
        Physical,
        Magical,
        True             // 방어 무시
    }

    // ════════════════════════════════════════════════════
    //  장비 / 아이템
    // ════════════════════════════════════════════════════
    public enum ItemType
    {
        Weapon,
        Armor,
        Accessory,
        Consumable
    }

    public enum Rarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    // ════════════════════════════════════════════════════
    //  스탯
    // ════════════════════════════════════════════════════
    

    // ════════════════════════════════════════════════════
    //  레이어 (Physics 충돌 매트릭스)
    // ════════════════════════════════════════════════════
    public static class Layer
    {
        public const int Player = 6;
        public const int Enemy = 7;
        public const int Ground = 8;
        public const int PlayerAttack = 9;
        public const int EnemyAttack = 10;
        public const int Interactable = 11;

        // LayerMask 헬퍼
        public static int PlayerMask => 1 << Player;
        public static int EnemyMask => 1 << Enemy;
        public static int GroundMask => 1 << Ground;
        public static int PlayerAttackMask => 1 << PlayerAttack;
        public static int EnemyAttackMask => 1 << EnemyAttack;
        public static int DamageableMask => PlayerMask | EnemyMask;
    }

    // ════════════════════════════════════════════════════
    //  태그
    // ════════════════════════════════════════════════════
    public static class Tag
    {
        public const string Player = "Player";
        public const string Enemy = "Enemy";
        public const string Item = "Item";
        public const string HitBox = "HitBox";
    }

    // ════════════════════════════════════════════════════
    //  씬 이름
    // ════════════════════════════════════════════════════
    public static class Scene
    {
        public const string Main = "Main";
        public const string Stage01 = "Stage_01";
        public const string Test = "Test";
    }

    // ════════════════════════════════════════════════════
    //  애니메이션 파라미터 해시
    //  Animator.StringToHash 비용을 줄이기 위해 캐싱합니다.
    // ════════════════════════════════════════════════════
    public static class AnimParam
    {
        // Animator.SetFloat / SetBool / SetTrigger에 사용
        public static readonly int Speed = UnityEngine.Animator.StringToHash("Speed");
        public static readonly int IsGrounded = UnityEngine.Animator.StringToHash("IsGrounded");
        public static readonly int Attack = UnityEngine.Animator.StringToHash("Attack");
        public static readonly int AttackIndex = UnityEngine.Animator.StringToHash("AttackIndex");
        public static readonly int Dodge = UnityEngine.Animator.StringToHash("Dodge");
        public static readonly int Hit = UnityEngine.Animator.StringToHash("Hit");
        public static readonly int Die = UnityEngine.Animator.StringToHash("Die");
        public static readonly int Skill = UnityEngine.Animator.StringToHash("Skill");
        public static readonly int SkillIndex = UnityEngine.Animator.StringToHash("SkillIndex");
        public static readonly int IsArmed = UnityEngine.Animator.StringToHash("IsArmed");
    }

    // ════════════════════════════════════════════════════
    //  게임 밸런스 상수
    // ════════════════════════════════════════════════════
    public static class Balance
    {
        public const float DefaultGravity = -20f;
        public const float DodgeInvincibleTime = 0.3f;
        public const float HitStunDuration = 0.4f;
        public const float ComboResetTime = 1.2f;
        public const int MaxComboCount = 3;
        public const float KnockbackForce = 5f;
    }
}