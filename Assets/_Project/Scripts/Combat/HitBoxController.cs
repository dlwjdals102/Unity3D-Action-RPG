using UnityEngine;

/// <summary>
/// PlayerAnimator의 히트 프레임 이벤트와 HitBox를 연결하는 컨트롤러.
/// Player 오브젝트에 부착하여, 공격 타이밍에 HitBox를 ON/OFF 합니다.
/// 
/// [구조]
/// PlayerAnimator.OnAttackHitFrame → HitBoxController → HitBox.Enable
/// PlayerAnimator.OnAttackEnd      → HitBoxController → HitBox.Disable
/// </summary>
public class HitBoxController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private HitBox _weaponHitBox;
    [SerializeField] private PlayerAnimator _playerAnimator;

    [Header("Combo Damage Multipliers")]
    [SerializeField] private float[] _comboDamageMultipliers = { 1.0f, 1.2f, 1.5f };

    private int _currentComboIndex = 0;
    private bool _suppressed = false;

    /// <summary>
    /// HitBox 작동을 억제합니다. 스킬 상태 등에서
    /// 무기 HitBox가 작동하면 안 될 때 사용합니다.
    /// </summary>
    public void SetSuppressed(bool suppressed)
    {
        _suppressed = suppressed;
        if (suppressed)
            _weaponHitBox?.DisableHitBox();
    }

    private void Awake()
    {
        if (_playerAnimator == null)
            _playerAnimator = GetComponent<PlayerAnimator>();
    }

    private void OnEnable()
    {
        if (_playerAnimator != null)
        {
            _playerAnimator.OnAttackHitFrame += OnHitFrame;
            _playerAnimator.OnAttackEnd += OnAttackEnd;
        }
    }

    private void OnDisable()
    {
        if (_playerAnimator != null)
        {
            _playerAnimator.OnAttackHitFrame -= OnHitFrame;
            _playerAnimator.OnAttackEnd -= OnAttackEnd;
        }
    }

    /// <summary>현재 콤보 인덱스를 설정합니다 (AttackState에서 호출).</summary>
    public void SetComboIndex(int index)
    {
        _currentComboIndex = index;
    }

    private void OnHitFrame()
    {
        if (_weaponHitBox == null) return;
        if (_suppressed) return;

        _weaponHitBox.EnableHitBox();

        // 짧은 시간 후 자동 비활성화 (판정 프레임 제한)
        CancelInvoke(nameof(AutoDisableHitBox));
        Invoke(nameof(AutoDisableHitBox), 0.15f);
    }

    private void OnAttackEnd()
    {
        if (_weaponHitBox != null)
            _weaponHitBox.DisableHitBox();
    }

    private void AutoDisableHitBox()
    {
        if (_weaponHitBox != null)
            _weaponHitBox.DisableHitBox();
    }
}