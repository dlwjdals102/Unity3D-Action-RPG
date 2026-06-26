using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 머리 위 체력 바 (World Space). 모든 적 공용.
/// 적이 전투 상태(IsInCombat)일 때만 표시하고, 항상 카메라를 향한다(빌보드).
/// EnemyHealth 의 피격/사망 이벤트로 fillAmount 를 갱신한다.
/// 
/// 적 프리팹의 머리 위에 World Space Canvas 로 배치. 같은 적의 컴포넌트들을
/// 부모에서 자동으로 찾는다(GetComponentInParent).
/// </summary>
public class EnemyWorldHealthBar : MonoBehaviour
{
    [Header("References (비우면 부모에서 자동 탐색)")]
    [Tooltip("적의 EnemyHealth")]
    [SerializeField] private EnemyHealth _health;

    [Tooltip("적의 StateMachine (전투 상태 판단)")]
    [SerializeField] private EnemyStateMachineBase _stateMachine;

    [Header("UI")]
    [Tooltip("체력 Fill 이미지 (Image Type: Filled)")]
    [SerializeField] private Image _healthFill;

    [Tooltip("바 루트 (표시/숨김 토글 대상)")]
    [SerializeField] private GameObject _barRoot;

    private Camera _camera;

    private void Awake()
    {
        // 부모에서 자동 탐색 (프리팹에 직접 할당돼 있으면 그대로 사용)
        if (_health == null) _health = GetComponentInParent<EnemyHealth>();
        if (_stateMachine == null) _stateMachine = GetComponentInParent<EnemyStateMachineBase>();

        _camera = Camera.main;

        SetVisible(false);  // 시작은 숨김 (전투 시 표시)
    }

    private void OnEnable()
    {
        if (_health != null)
        {
            _health.OnDamaged += UpdateBar;
            _health.OnDeath += HandleDeath;
        }
    }

    private void OnDisable()
    {
        if (_health != null)
        {
            _health.OnDamaged -= UpdateBar;
            _health.OnDeath -= HandleDeath;
        }
    }

    private void LateUpdate()
    {
        // 전투 상태에 따라 표시/숨김
        bool inCombat = _stateMachine != null && _stateMachine.IsInCombat;
        SetVisible(inCombat);

        // 표시 중이면 카메라를 향함 (빌보드)
        if (inCombat && _barRoot != null && _camera != null)
        {
            // 카메라와 같은 방향을 보게 하여 항상 정면으로 읽히게 함
            _barRoot.transform.rotation = _camera.transform.rotation;
        }
    }

    private void UpdateBar()
    {
        if (_healthFill == null || _health == null) return;
        if (_health.MaxHealth <= 0) return;

        _healthFill.fillAmount = (float)_health.CurrentHealth / _health.MaxHealth;
    }

    private void HandleDeath()
    {
        UpdateBar();        // 0 으로
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