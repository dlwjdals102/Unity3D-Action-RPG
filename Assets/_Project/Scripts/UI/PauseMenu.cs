using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 일시정지 메뉴. ESC 로 토글하되, 다른 패널(상점/인벤토리)이 열려 있으면 양보한다.
/// Time.timeScale 로 게임 정지 + UIInputLock 으로 카메라/전투 입력 차단.
/// Resume / 타이틀로 돌아가기 버튼 제공.
///
/// ESC 중재: ConsumeCancel() 1회성 소비 + UIInputLock.IsOpen 가드.
/// - Pause 중      → ESC 로 Resume
/// - 패널 열림     → ESC 양보(그 패널이 닫힘), Pause 안 열림
/// - 아무것도 없음 → ESC 로 Pause
/// </summary>
public class PauseMenu : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private PlayerController _controller;

    [Tooltip("표시/숨김 토글 대상 (Pause 오버레이 루트)")]
    [SerializeField] private GameObject _menuRoot;

    [Header("Scene")]
    [Tooltip("타이틀 씬 이름")]
    [SerializeField] private string _titleSceneName = "Scene_Title";

    private bool _isPaused;

    private void Awake()
    {
        if (_controller == null) _controller = FindFirstObjectByType<PlayerController>();
        if (_menuRoot != null) _menuRoot.SetActive(false);
    }

    private void Update()
    {
        if (_controller == null) return;

        if (_isPaused)
        {
            // Pause 중 ESC → Resume
            if (_controller.ConsumeCancel()) Resume();
            return;
        }

        // 다른 패널(상점/인벤토리)이 열려 있으면 ESC 는 그쪽 몫 → 양보 (소비하지 않음)
        if (UIInputLock.IsOpen) return;

        // 아무것도 안 열림 + ESC → Pause
        if (_controller.ConsumeCancel()) Pause();
    }

    private void Pause()
    {
        _isPaused = true;
        if (_menuRoot != null) _menuRoot.SetActive(true);

        UIInputLock.Push();      // 카메라/전투 입력 차단
        Time.timeScale = 0f;     // 게임 정지

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>Resume 버튼 / ESC 에서 호출.</summary>
    public void Resume()
    {
        _isPaused = false;
        if (_menuRoot != null) _menuRoot.SetActive(false);

        Time.timeScale = 1f;     // 정지 해제
        UIInputLock.Pop();

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>타이틀로 돌아가기 버튼에서 호출.</summary>
    public void ReturnToTitle()
    {
        // 씬 로드 전에 timeScale 복구 (안 하면 타이틀 씬이 멈춘 채 로드됨)
        Time.timeScale = 1f;
        // Pause 에서 Push 한 UIInputLock 균형 맞춤 (static 카운트가 씬 넘어 남지 않게)
        UIInputLock.Pop();
        SceneManager.LoadScene(_titleSceneName);
    }
}