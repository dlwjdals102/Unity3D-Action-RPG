using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 스탯/경험치 HUD. 레벨, EXP바, 스탯 수치를 표시합니다.
/// </summary>
public class StatsHUD : MonoBehaviour
{
    [Header("Level & EXP")]
    [SerializeField] private TextMeshProUGUI _levelText;
    [SerializeField] private Image _expFillImage;
    [SerializeField] private TextMeshProUGUI _expText;

    [Header("Level Up Effect")]
    [SerializeField] private GameObject _levelUpPopup;
    [SerializeField] private float _popupDuration = 2f;

    private PlayerStats _stats;

    private void Start()
    {
        GameObject player = GameObject.FindGameObjectWithTag(Define.Tag.Player);
        if (player != null)
        {
            _stats = player.GetComponent<PlayerStats>();

            if (_stats != null)
            {
                _stats.OnExpChanged += OnExpChanged;
                _stats.OnLevelUp += OnLevelUp;

                // 초기값 표시
                UpdateDisplay();
            }
        }

        if (_levelUpPopup != null)
            _levelUpPopup.SetActive(false);
    }

    private void OnDestroy()
    {
        if (_stats != null)
        {
            _stats.OnExpChanged -= OnExpChanged;
            _stats.OnLevelUp -= OnLevelUp;
        }
    }

    private void OnExpChanged(int current, int required)
    {
        UpdateDisplay();
    }

    private void OnLevelUp(int newLevel)
    {
        UpdateDisplay();

        // 레벨업 팝업
        if (_levelUpPopup != null)
        {
            _levelUpPopup.SetActive(true);
            CancelInvoke(nameof(HidePopup));
            Invoke(nameof(HidePopup), _popupDuration);
        }
    }

    private void UpdateDisplay()
    {
        if (_stats == null) return;

        if (_levelText != null)
            _levelText.text = $"Lv. {_stats.Level}";

        if (_expFillImage != null)
            _expFillImage.fillAmount = _stats.ExpRatio;

        if (_expText != null)
            _expText.text = $"{_stats.CurrentExp} / {_stats.ExpToNextLevel}";
    }

    private void HidePopup()
    {
        if (_levelUpPopup != null)
            _levelUpPopup.SetActive(false);
    }
}