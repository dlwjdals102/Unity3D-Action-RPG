using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.TextCore.Text;
using UnityEngine.UI;

/// <summary>
/// 게임 UI 매니저. 사망 화면과 일시정지 메뉴를 관리합니다.
/// HUD_Canvas에 부착합니다.
/// </summary>
public class GameUIManager : MonoBehaviour
{
    // ════════════════════════════════════════════════════
    //  사망 화면
    // ════════════════════════════════════════════════════

    [Header("Death Screen")]
    [SerializeField] private GameObject _deathPanel;
    [SerializeField] private TextMeshProUGUI _deathText;
    [SerializeField] private Button _restartButton;

    // ════════════════════════════════════════════════════
    //  일시정지 메뉴
    // ════════════════════════════════════════════════════

    [Header("Pause Menu")]
    [SerializeField] private GameObject _pausePanel;
    [SerializeField] private Button _resumeButton;
    [SerializeField] private Button _quitButton;

    // ── 내부 ──
    private bool _isPaused = false;

    // ════════════════════════════════════════════════════
    //  초기화
    // ════════════════════════════════════════════════════

    private void Start()
    {
        // 패널 숨기기
        if (_deathPanel != null) _deathPanel.SetActive(false);
        if (_pausePanel != null) _pausePanel.SetActive(false);

        // 버튼 이벤트 연결
        if (_restartButton != null)
            _restartButton.onClick.AddListener(OnRestartClicked);
        if (_resumeButton != null)
            _resumeButton.onClick.AddListener(OnResumeClicked);
        if (_quitButton != null)
            _quitButton.onClick.AddListener(OnQuitClicked);

        // 플레이어 사망 이벤트 구독
        if (GameManager.HasInstance)
            GameManager.Instance.OnPlayerDeath += ShowDeathScreen;
    }

    private void OnDestroy()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.OnPlayerDeath -= ShowDeathScreen;
    }

    private void Update()
    {
        // ESC 키 — 일시정지 토글
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            // 사망 화면이 뜬 상태에서는 일시정지 불가
            if (_deathPanel != null && _deathPanel.activeSelf) return;

            if (_isPaused)
                OnResumeClicked();
            else
                ShowPauseMenu();
        }
    }

    // ════════════════════════════════════════════════════
    //  사망 화면
    // ════════════════════════════════════════════════════

    private void ShowDeathScreen()
    {
        if (_deathPanel == null) return;

        // 사망 연출: 슬로우 모션 → 패널 표시 → 정지
        StartCoroutine(DeathSequence());
    }

    private System.Collections.IEnumerator DeathSequence()
    {
        // 1단계: 슬로우 모션
        Time.timeScale = 0.5f;

        // 플레이어 Animator를 찾아서 Die 애니메이션 완료 대기
        GameObject player = GameObject.FindGameObjectWithTag(Define.Tag.Player);
        Animator playerAnimator = player != null ? player.GetComponent<Animator>() : null;

        if (playerAnimator != null)
        {
            // Die 애니메이션이 시작될 때까지 대기
            yield return new WaitForSecondsRealtime(0.2f);

            // Die 애니메이션 완료 대기 (최대 5초 안전장치)
            float safetyTimer = 0f;
            while (safetyTimer < 5f)
            {
                var stateInfo = playerAnimator.GetCurrentAnimatorStateInfo(0);

                // normalizedTime >= 0.9면 애니메이션이 거의 끝남
                if (stateInfo.normalizedTime >= 0.9f)
                    break;

                safetyTimer += Time.unscaledDeltaTime;
                yield return null;
            }
        }
        else
        {
            // Animator 없으면 고정 시간 대기
            yield return new WaitForSecondsRealtime(2f);
        }

        // 2단계: 완전 정지 + UI 표시
        Time.timeScale = 0f;

        if (_deathPanel != null)
            _deathPanel.SetActive(true);

        if (_deathText != null)
            _deathText.text = "YOU DIED";

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ════════════════════════════════════════════════════
    //  일시정지 메뉴
    // ════════════════════════════════════════════════════

    private void ShowPauseMenu()
    {
        _isPaused = true;

        if (_pausePanel != null) _pausePanel.SetActive(true);

        Time.timeScale = 0f;

        // 이동 정지 + 입력 억제
        GameObject player = GameObject.FindGameObjectWithTag(Define.Tag.Player);
        if (player != null)
        {
            var controller = player.GetComponent<PlayerController>();
            if (controller != null)
                controller.StopMovement();

            var input = player.GetComponent<PlayerInputHandler>();
            if (input != null)
            {
                input.InputSuppressed = true;
                input.ClearAllBuffers();
            }
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    // ════════════════════════════════════════════════════
    //  버튼 핸들러
    // ════════════════════════════════════════════════════

    private void OnResumeClicked()
    {
        _isPaused = false;

        if (_pausePanel != null) _pausePanel.SetActive(false);

        Time.timeScale = 1f;

        // 입력 억제 해제 + 버퍼 클리어
        GameObject player = GameObject.FindGameObjectWithTag(Define.Tag.Player);
        if (player != null)
        {
            var input = player.GetComponent<PlayerInputHandler>();
            if (input != null)
            {
                input.ClearAllBuffers();
                // 마우스가 떼어질 때까지 억제 유지
                StartCoroutine(UnsuppressAfterMouseRelease(input));
            }
        }

        // 인벤토리가 열려있지 않으면 커서 잠금
        var inventoryUI = GetComponent<InventoryUI>();
        if (inventoryUI == null || !inventoryUI.IsOpen)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
    }

    private System.Collections.IEnumerator UnsuppressAfterMouseRelease(PlayerInputHandler input)
    {
        // 마우스 좌클릭이 완전히 떼어질 때까지 대기
        while (UnityEngine.InputSystem.Mouse.current != null &&
               UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
        {
            yield return null;
        }

        // 추가 1프레임 대기
        yield return null;

        if (input != null)
        {
            input.ClearAllBuffers();
            input.InputSuppressed = false;
        }
    }

    private void OnRestartClicked()
    {
        Time.timeScale = 1f;

        if (GameManager.HasInstance)
            GameManager.Instance.RestartScene();
    }

    private void OnQuitClicked()
    {
        Time.timeScale = 1f;

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}