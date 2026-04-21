using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 타이틀 화면 UI.
/// "시작하기" 버튼을 누르면 게임 씬으로 전환합니다.
/// 
/// [사용법]
/// Title 씬의 Canvas에 부착합니다.
/// </summary>
public class TitleUI : MonoBehaviour
{
    [Header("Buttons")]
    [SerializeField] private Button _startButton;
    [SerializeField] private Button _quitButton;

    [Header("Scene")]
    [SerializeField] private string _gameSceneName = "Test";

    private void Start()
    {
        // 커서 표시
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Time.timeScale = 1f;

        if (_startButton != null)
            _startButton.onClick.AddListener(OnStartClicked);

        if (_quitButton != null)
            _quitButton.onClick.AddListener(OnQuitClicked);
    }

    private void OnStartClicked()
    {
        if (GameManager.HasInstance)
            GameManager.Instance.LoadScene(_gameSceneName);
        else
            UnityEngine.SceneManagement.SceneManager.LoadSceneAsync(_gameSceneName);
    }

    private void OnQuitClicked()
    {
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
    }
}