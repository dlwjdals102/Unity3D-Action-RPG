using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 HUD. HP바와 스킬 쿨다운을 표시합니다.
/// Canvas 아래에 배치하여 사용합니다.
/// 
/// [구조]
/// Canvas (Screen Space - Overlay)
///   └── PlayerHUD
///       ├── HP Bar
///       └── Skill Slots
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  HP Bar 참조
    // ════════════════════════════════════════════════════

    [Header("HP Bar")]
    [SerializeField] private Image _hpFillImage;
    [SerializeField] private Image _hpDamageFillImage;
    [SerializeField] private TextMeshProUGUI _hpText;

    [Header("HP Bar Settings")]
    [SerializeField] private float _damageFillSpeed = 2f;

    // ════════════════════════════════════════════════════
    //  Skill Slots 참조
    // ════════════════════════════════════════════════════

    [Header("Skill Slots")]
    [SerializeField] private Image _skill1CooldownFill;
    [SerializeField] private Image _skill1Icon;
    [SerializeField] private TextMeshProUGUI _skill1CooldownText;
    [SerializeField] private Image _skill2CooldownFill;
    [SerializeField] private Image _skill2Icon;
    [SerializeField] private TextMeshProUGUI _skill2CooldownText;

    // ════════════════════════════════════════════════════
    //  내부
    // ════════════════════════════════════════════════════

    private PlayerHealth _playerHealth;
    private SkillExecutor _skillExecutor;
    private float _targetHpRatio = 1f;
    private float _currentDamageFill = 1f;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Start()
    {
        // 플레이어 찾기
        GameObject player = GameObject.FindGameObjectWithTag(Define.Tag.Player);
        if (player != null)
        {
            _playerHealth = player.GetComponent<PlayerHealth>();
            _skillExecutor = player.GetComponent<SkillExecutor>();

            if (_playerHealth != null)
            {
                _playerHealth.OnHpChanged += OnHpChanged;
                UpdateHpBar(_playerHealth.CurrentHp, _playerHealth.MaxHp);
            }

            // 스킬 아이콘 설정
            if (_skillExecutor != null)
            {
                SetupSkillIcons();
            }
        }
    }

    private void OnDestroy()
    {
        if (_playerHealth != null)
            _playerHealth.OnHpChanged -= OnHpChanged;
    }

    // ════════════════════════════════════════════════════
    //  업데이트
    // ════════════════════════════════════════════════════

    private void Update()
    {
        UpdateDamageFill();
        UpdateSkillCooldowns();
    }

    // ════════════════════════════════════════════════════
    //  HP Bar
    // ════════════════════════════════════════════════════

    private void OnHpChanged(float currentHp, float maxHp)
    {
        UpdateHpBar(currentHp, maxHp);
    }

    private void UpdateHpBar(float currentHp, float maxHp)
    {
        _targetHpRatio = maxHp > 0 ? currentHp / maxHp : 0f;

        // 즉시 HP바 업데이트
        if (_hpFillImage != null)
            _hpFillImage.fillAmount = _targetHpRatio;

        // HP 텍스트 업데이트
        if (_hpText != null)
            _hpText.text = $"{Mathf.CeilToInt(currentHp)} / {Mathf.CeilToInt(maxHp)}";
    }

    private void UpdateDamageFill()
    {
        // 대미지 필 (빨간 바)이 천천히 줄어드는 연출
        if (_hpDamageFillImage == null) return;

        if (_currentDamageFill > _targetHpRatio)
        {
            _currentDamageFill -= _damageFillSpeed * Time.deltaTime;
            _currentDamageFill = Mathf.Max(_currentDamageFill, _targetHpRatio);
        }
        else
        {
            _currentDamageFill = _targetHpRatio;
        }

        _hpDamageFillImage.fillAmount = _currentDamageFill;
    }

    // ════════════════════════════════════════════════════
    //  Skill Cooldowns
    // ════════════════════════════════════════════════════

    private void SetupSkillIcons()
    {
        SkillData skill1 = _skillExecutor.GetSkillData(0);
        SkillData skill2 = _skillExecutor.GetSkillData(1);

        if (skill1 != null && _skill1Icon != null && skill1.icon != null)
            _skill1Icon.sprite = skill1.icon;

        if (skill2 != null && _skill2Icon != null && skill2.icon != null)
            _skill2Icon.sprite = skill2.icon;
    }

    private void UpdateSkillCooldowns()
    {
        if (_skillExecutor == null) return;

        // Skill 1
        UpdateSkillSlot(
            0,
            _skill1CooldownFill,
            _skill1CooldownText
        );

        // Skill 2
        UpdateSkillSlot(
            1,
            _skill2CooldownFill,
            _skill2CooldownText
        );
    }

    private void UpdateSkillSlot(int slotIndex, Image cooldownFill, TextMeshProUGUI cooldownText)
    {
        float ratio = _skillExecutor.GetCooldownRatio(slotIndex);
        float remaining = _skillExecutor.GetRemainingCooldown(slotIndex);

        if (cooldownFill != null)
            cooldownFill.fillAmount = ratio;

        if (cooldownText != null)
        {
            if (remaining > 0f)
            {
                cooldownText.gameObject.SetActive(true);
                cooldownText.text = Mathf.CeilToInt(remaining).ToString();
            }
            else
            {
                cooldownText.gameObject.SetActive(false);
            }
        }
    }
}