using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 화면 상단 타겟 체력 바. 락온한 적(LockOnSystem.CurrentTarget)의 체력을 표시한다.
/// "현재 락온 타겟" 을 따라가며, 타겟이 바뀌면 구독을 옮긴다.
/// 
/// EnemyHealth 의 이벤트(OnDamaged/OnDeath)는 인자가 없어 콜백에서 직접 조회해 갱신.
/// 락온 해제/타겟 사망 시 숨김.
/// </summary>
public class TargetHealthBar : MonoBehaviour
{
    [Header("References")]
    [Tooltip("락온 타겟을 제공하는 LockOnSystem")]
    [SerializeField] private LockOnSystem _lockOnSystem;

    [Header("UI")]
    [Tooltip("체력 Fill 이미지 (Image Type: Filled)")]
    [SerializeField] private Image _healthFill;

    [Tooltip("바 루트 (표시/숨김 토글 대상)")]
    [SerializeField] private GameObject _barRoot;

    [Tooltip("적 이름 텍스트 (선택)")]
    [SerializeField] private Text _nameText;

    private EnemyHealth _currentHealth;   // 현재 구독 중인 적 체력
    private Transform _lastTarget;        // 타겟 변경 감지용

    private void Awake()
    {
        SetVisible(false);
    }

    private void OnDisable()
    {
        // 씬 종료/비활성 시 구독 정리
        Unsubscribe();
    }

    private void Update()
    {
        if (_lockOnSystem == null) return;

        Transform target = _lockOnSystem.CurrentTarget;

        // 타겟이 바뀌었을 때만 구독 갱신 (매 프레임 불필요한 처리 방지)
        if (target != _lastTarget)
        {
            _lastTarget = target;
            OnTargetChanged(target);
        }
    }

    /// <summary>락온 타겟 변경 시: 이전 구독 해제 + 새 타겟 구독/표시.</summary>
    private void OnTargetChanged(Transform target)
    {
        Unsubscribe();

        if (target == null)
        {
            SetVisible(false);
            return;
        }

        // 타겟에서 EnemyHealth 탐색 (적이 아니면 표시 안 함)
        _currentHealth = target.GetComponent<EnemyHealth>();
        if (_currentHealth == null)
        {
            SetVisible(false);
            return;
        }

        // 새 타겟 구독 + 표시
        _currentHealth.OnDamaged += UpdateBar;
        _currentHealth.OnDeath += HandleTargetDeath;

        if (_nameText != null)
        {
            _nameText.text = target.name;  // 적 이름 (또는 별도 표시명)
        }

        SetVisible(true);
        UpdateBar();  // 현재 체력 즉시 반영
    }

    private void Unsubscribe()
    {
        if (_currentHealth != null)
        {
            _currentHealth.OnDamaged -= UpdateBar;
            _currentHealth.OnDeath -= HandleTargetDeath;
            _currentHealth = null;
        }
    }

    private void UpdateBar()
    {
        if (_healthFill == null || _currentHealth == null) return;
        if (_currentHealth.MaxHealth <= 0) return;

        _healthFill.fillAmount = (float)_currentHealth.CurrentHealth / _currentHealth.MaxHealth;
    }

    private void HandleTargetDeath()
    {
        UpdateBar();        // 0
        SetVisible(false);  // 사망 시 숨김
    }

    private void SetVisible(bool visible)
    {
        if (_barRoot != null && _barRoot.activeSelf != visible)
        {
            _barRoot.SetActive(visible);
        }
    }
}