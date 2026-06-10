using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 타이틀 화면 메뉴. 새 게임 / 이어하기 / 종료.
/// - 새 게임: 저장 삭제 후 게임 씬 로드 (처음부터)
/// - 이어하기: 게임 씬 로드 (SaveCoordinator 가 시작 시 자동 로드)
/// - 저장 파일이 없으면 이어하기 버튼 비활성.
/// 메뉴는 Unity 표준 Button 사용 (게임 내 슬롯 UI 와 달리 메뉴는 Button 이 적합).
/// </summary>
public class TitleMenu : MonoBehaviour
{
    [Tooltip("게임 씬 이름 (Build Settings 등록 필요)")]
    [SerializeField] private string _gameSceneName = "Scene_PlayerTest";

    [Tooltip("이어하기 버튼 (저장 없으면 비활성)")]
    [SerializeField] private Button _continueButton;

    private void Start()
    {
        // 저장 파일 유무로 이어하기 활성/비활성
        if (_continueButton != null && SaveManager.Instance != null)
        {
            _continueButton.interactable = SaveManager.Instance.HasSave();
        }
    }

    /// <summary>새 게임: 저장 삭제 후 처음부터. (버튼 OnClick 연결)</summary>
    public void NewGame()
    {
        if (SaveManager.Instance != null) SaveManager.Instance.DeleteSave();
        SceneManager.LoadScene(_gameSceneName);
    }

    /// <summary>이어하기: 게임 씬 로드 (자동 로드가 진행 복원). (버튼 OnClick 연결)</summary>
    public void ContinueGame()
    {
        SceneManager.LoadScene(_gameSceneName);
    }

    /// <summary>게임 종료. (버튼 OnClick 연결)</summary>
    public void QuitGame()
    {
        Debug.Log("[Title] 게임 종료");
        Application.Quit();  // 에디터에선 동작 안 함 (빌드에서)
    }
}