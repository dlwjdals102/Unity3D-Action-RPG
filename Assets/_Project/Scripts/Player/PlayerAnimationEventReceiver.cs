using UnityEngine;

/// <summary>
/// Model GameObject 에 부착되어 Animation Event 를 받는 라우터.
/// Animator 가 부착된 GameObject 에서 발사된 Animation Event 를 
/// 부모의 PlayerAnimator 로 전달한다.
/// 향후 Attack, Footstep 등 다양한 Animation Event 가 추가될 때 한 곳에서 관리.
/// </summary>
public class PlayerAnimationEventReceiver : MonoBehaviour
{
    private PlayerAnimator _playerAnimator;

    private void Awake()
    {
        _playerAnimator = GetComponentInParent<PlayerAnimator>();

        if (_playerAnimator == null)
        {
            Debug.LogError("[PlayerAnimationEventReceiver] PlayerAnimator not found in parent!");
        }
    }

    // ========================================================================
    // === Animation Event Callbacks ===
    // 각 메서드는 Mixamo 클립의 Animation Event 에서 호출된다.
    // ========================================================================

    /// <summary>
    /// Land (Hard Landing) 애니메이션의 종료 시점에 호출.
    /// Mixamo 클립의 끝 부분에 Animation Event 로 추가.
    /// </summary>
    public void OnLandAnimationEnd()
    {
        _playerAnimator?.OnLandAnimationFinished();
    }

    /// <summary>
    /// Dodge (Roll) 애니메이션의 종료 시점에 호출.
    /// Mixamo 클립의 끝 부분에 Animation Event 로 추가.
    /// </summary>
    public void OnDodgeAnimationEnd()
    {
        _playerAnimator?.OnDodgeAnimationFinished();
    }
}