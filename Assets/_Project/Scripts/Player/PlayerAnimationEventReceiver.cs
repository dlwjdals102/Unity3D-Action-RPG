using UnityEngine;

/// <summary>
/// Model GameObject 에 부착되어 Animation Event 를 받는 라우터.
/// Player 관련 모든 Animation Event 를 한 곳에서 수신하여 적절한 컴포넌트로 분배한다.
/// 
/// 라우팅 대상:
/// - PlayerAnimator: 애니메이션 종료 추적 (Land, Dodge, Attack 등)
/// - PlayerAttacker: 공격 타격 처리 (OnAttackHit)
/// 
/// 향후 다른 Animation Event (예: Footstep → SoundManager) 도 같은 패턴으로 추가 가능.
/// </summary>
public class PlayerAnimationEventReceiver : MonoBehaviour
{
    private PlayerAnimator _playerAnimator;
    private PlayerAttacker _playerAttacker;

    private void Awake()
    {
        _playerAnimator = GetComponentInParent<PlayerAnimator>();
        _playerAttacker = GetComponentInParent<PlayerAttacker>();

        if (_playerAnimator == null)
        {
            Debug.LogError("[PlayerAnimationEventReceiver] PlayerAnimator not found in parent!");
        }

        if (_playerAttacker == null)
        {
            Debug.LogError("[PlayerAnimationEventReceiver] PlayerAttacker not found in parent!");
        }
    }

    // ========================================================================
    // === Animation Event Callbacks ===
    // 각 메서드는 Mixamo 클립의 Animation Event 에서 호출된다.
    // ========================================================================

    /// <summary>
    /// Land (Hard Landing) 애니메이션의 종료 시점에 호출.
    /// </summary>
    public void OnLandAnimationEnd()
    {
        _playerAnimator?.OnLandAnimationFinished();
    }

    /// <summary>
    /// Dodge (Roll) 애니메이션의 종료 시점에 호출.
    /// </summary>
    public void OnDodgeAnimationEnd()
    {
        _playerAnimator?.OnDodgeAnimationFinished();
    }

    /// <summary>
    /// 공격 콤보 윈도우 시작 시점에 호출.
    /// </summary>
    public void OnComboWindowOpen()
    {
        _playerAnimator?.OnComboWindowOpened();
    }

    /// <summary>
    /// 공격 콤보 윈도우 종료 시점에 호출.
    /// </summary>
    public void OnComboWindowClose()
    {
        _playerAnimator?.OnComboWindowClosed();
    }

    /// <summary>
    /// 공격 애니메이션 전체 종료 시점에 호출.
    /// </summary>
    public void OnAttackAnimationEnd()
    {
        _playerAnimator?.OnAttackAnimationFinished();
    }

    /// <summary>
    /// 공격 타격 시점에 호출.
    /// PlayerAttacker 가 OverlapSphere 로 한 프레임 검사하여 적 감지 + 데미지 적용.
    /// </summary>
    public void OnAttackHit()
    {
        _playerAttacker?.PerformHit();
    }
}