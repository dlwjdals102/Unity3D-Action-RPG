using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 적 머리 위에 표시되는 월드 스페이스 HP바.
/// EnemyController의 OnDamaged 이벤트에 반응하여 HP를 표시합니다.
/// 
/// [사용법]
/// 1. 적 오브젝트 자식으로 Canvas (World Space) 생성
/// 2. Canvas 안에 HP바 UI 배치
/// 3. 이 스크립트를 Canvas에 부착
/// </summary>
public class EnemyHealthBar : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Image _fillImage;
    [SerializeField] private Image _damageFillImage;

    [Header("Settings")]
    [SerializeField] private float _damageFillSpeed = 3f;
    [SerializeField] private float _showDuration = 3f;
    [SerializeField] private Vector3 _offset = new Vector3(0f, 2.2f, 0f);

    // ── 내부 ──
    private EnemyController _enemy;
    private Transform _cameraTransform;
    private CanvasGroup _canvasGroup;
    private float _targetFill = 1f;
    private float _currentDamageFill = 1f;
    private float _hideTimer;
    private bool _isVisible = false;

    private void Awake()
    {
        _enemy = GetComponentInParent<EnemyController>();
        _canvasGroup = GetComponent<CanvasGroup>();

        if (_canvasGroup == null)
            _canvasGroup = gameObject.AddComponent<CanvasGroup>();

        _canvasGroup.alpha = 0f;
    }

    private void Start()
    {
        if (Camera.main != null)
            _cameraTransform = Camera.main.transform;

        if (_enemy != null)
            _enemy.OnDamaged += OnEnemyDamaged;
    }

    private void OnDestroy()
    {
        if (_enemy != null)
            _enemy.OnDamaged -= OnEnemyDamaged;
    }

    private void LateUpdate()
    {
        // 카메라를 향해 회전 (빌보드)
        if (_cameraTransform != null)
            transform.forward = _cameraTransform.forward;

        // 위치 오프셋
        if (_enemy != null)
            transform.position = _enemy.transform.position + _offset;

        // 대미지 필 애니메이션
        if (_currentDamageFill > _targetFill)
        {
            _currentDamageFill -= _damageFillSpeed * Time.deltaTime;
            _currentDamageFill = Mathf.Max(_currentDamageFill, _targetFill);
        }
        else
        {
            _currentDamageFill = _targetFill;
        }

        if (_damageFillImage != null)
            _damageFillImage.fillAmount = _currentDamageFill;

        // 일정 시간 후 숨기기
        if (_isVisible)
        {
            _hideTimer -= Time.deltaTime;
            if (_hideTimer <= 0f)
            {
                _isVisible = false;
                _canvasGroup.alpha = 0f;
            }
        }
    }

    private void OnEnemyDamaged(DamageData data)
    {
        if (_enemy == null) return;

        _targetFill = _enemy.CurrentHp / _enemy.MaxHp;

        if (_fillImage != null)
            _fillImage.fillAmount = _targetFill;

        // 표시
        _isVisible = true;
        _canvasGroup.alpha = 1f;
        _hideTimer = _showDuration;
    }
}