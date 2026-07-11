using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 타이틀 화면 메뉴. 새 게임 / 이어하기 / 종료.
/// - 새 게임: 저장 삭제 후 게임 씬 로드 (처음부터)
/// - 이어하기: 게임 씬 로드 (SaveCoordinator 가 시작 시 자동 로드)
/// - 저장 파일이 없으면 이어하기 버튼 비활성.
/// 메뉴는 Unity 표준 Button 사용.
/// </summary>
public class TitleMenu : MonoBehaviour
{
    [Tooltip("게임 씬 이름 (Build Settings 등록 필요)")]
    [SerializeField] private string _gameSceneName = "Scene_Game";

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

    /// <summary>게임 종료. (버튼 OnClick 연결) WebGL(브라우저)에선 종료가 불가능해 무시한다.</summary>
    public void QuitGame()
    {
#if UNITY_WEBGL
        return;  // 브라우저에선 창을 닫을 수 없음 → 멈춤 방지를 위해 무시
#else
        Application.Quit();  // 에디터에선 동작 안 함 (빌드에서만)
#endif
    }
}