using UnityEngine;

/// <summary>
/// 사망 연출 UI ("YOU DIED" 오버레이).
/// PlayerHealth.OnDeath 에 표시, PlayerRespawn.OnRespawned 에 숨김 (이벤트 쌍).
/// 제어 스크립트는 항상 활성인 곳에 두고 오버레이(_screenRoot)만 토글.
/// </summary>
public class DeathScreenUI : MonoBehaviour
{
    [SerializeField] private PlayerHealth _health;
    [SerializeField] private PlayerRespawn _respawn;

    [Tooltip("표시/숨김 토글 대상 (YOU DIED 오버레이 루트)")]
    [SerializeField] private GameObject _screenRoot;

    private void Awake()
    {
        if (_screenRoot != null) _screenRoot.SetActive(false);
    }

    private void OnEnable()
    {
        if (_health != null) _health.OnDeath += Show;
        if (_respawn != null) _respawn.OnRespawned += Hide;
    }

    private void OnDisable()
    {
        if (_health != null) _health.OnDeath -= Show;
        if (_respawn != null) _respawn.OnRespawned -= Hide;
    }

    private void Show()
    {
        if (_screenRoot != null) _screenRoot.SetActive(true);
    }

    private void Hide()
    {
        if (_screenRoot != null) _screenRoot.SetActive(false);
    }
}