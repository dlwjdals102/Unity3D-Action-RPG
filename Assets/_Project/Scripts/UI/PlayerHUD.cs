using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 플레이어 HUD - 체력/스태미나 바.
/// PlayerHealth/PlayerStamina 의 변경 이벤트를 구독해 Image fillAmount 를 갱신한다.
/// (폴링 대신 이벤트 구독 - 느슨한 결합)
/// 
/// 구독 타이밍 안전장치: OnEnable 에서 구독한 뒤 즉시 현재값으로 1회 갱신하여,
/// PlayerHealth/Stamina 의 초기 발행(Start)을 놓치더라도 바가 올바르게 시작된다.
/// </summary>
public class PlayerHUD : MonoBehaviour
{
    [Header("References")]
    [Tooltip("플레이어의 PlayerHealth")]
    [SerializeField] private PlayerHealth _playerHealth;

    [Tooltip("플레이어의 PlayerStamina")]
    [SerializeField] private PlayerStamina _playerStamina;

    [Header("Bars (Image Type: Filled)")]
    [Tooltip("체력 바 (Image, Fill)")]
    [SerializeField] private Image _healthFill;

    [Tooltip("스태미나 바 (Image, Fill)")]
    [SerializeField] private Image _staminaFill;

    private void OnEnable()
    {
        if (_playerHealth != null)
        {
            _playerHealth.OnHealthChanged += UpdateHealthBar;
            // 즉시 현재값 반영 (초기 발행 놓침 대비)
            UpdateHealthBar(_playerHealth.CurrentHealth, _playerHealth.MaxHealth);
        }

        if (_playerStamina != null)
        {
            _playerStamina.OnStaminaChanged += UpdateStaminaBar;
            UpdateStaminaBar(_playerStamina.CurrentStamina, _playerStamina.MaxStamina);
        }
    }

    private void OnDisable()
    {
        if (_playerHealth != null)
            _playerHealth.OnHealthChanged -= UpdateHealthBar;

        if (_playerStamina != null)
            _playerStamina.OnStaminaChanged -= UpdateStaminaBar;
    }

    private void UpdateHealthBar(int current, int max)
    {
        if (_healthFill == null || max <= 0) return;
        _healthFill.fillAmount = (float)current / max;
    }

    private void UpdateStaminaBar(float current, float max)
    {
        if (_staminaFill == null || max <= 0f) return;
        _staminaFill.fillAmount = current / max;
    }
}